using System.Reactive.Linq;
using System.Reactive.Subjects;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using UAUIngleza_plc.Interfaces;

namespace UAUIngleza_plc.Services
{
    public partial class PLCService(IConfigRepository configRepository) : IPlcService, IDisposable
    {
        private readonly IConfigRepository _configRepository = configRepository;
        private const ConnectionState Value = default;
        private readonly BehaviorSubject<ConnectionState> _connectionStatus = new(Value);
        private CancellationTokenSource? _reconnectCancellation;
        private IDisposable? _connectionStateSubscription;
        private Task? _reconnectTask;
        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public Sharp7Plc? Plc { get; private set; }
        public IObservable<ConnectionState> ConnectionStatus => _connectionStatus.AsObservable();
        public bool IsConnected => _connectionStatus.Value == ConnectionState.Connected;

        public async Task<bool> ConnectAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                var config = await _configRepository.GetOneAsync<Models.SystemConfiguration>(0);

                if (config == null || string.IsNullOrEmpty(config.IpAddress))
                {
                    _connectionStatus.OnNext(default);
                    return false;
                }

                CleanupConnection();

                Plc = new Sharp7Plc(config.IpAddress, config.Rack, config.Slot);

                var connectionTask = Plc.InitializeConnection();
                var timeoutTask = Task.Delay(5000);

                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _connectionStatus.OnNext(default);
                    CleanupConnection();
                    return false;
                }

                SubscribeToConnectionState();

                return true;
            }
            catch (Exception)
            {
                _connectionStatus.OnNext(default);
                CleanupConnection();
                return false;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private void SubscribeToConnectionState()
        {
            if (Plc == null)
                return;

            _connectionStateSubscription?.Dispose();

            _connectionStateSubscription = Plc
                .ConnectionState.DistinctUntilChanged()
                .Subscribe(
                    state =>
                    {
                        _connectionStatus.OnNext(state);

                        if (
                            state != ConnectionState.Connected
                            && _reconnectCancellation != null
                            && !_reconnectCancellation.IsCancellationRequested
                        )
                        {
                            StartReconnectLoop();
                        }
                    },
                    error =>
                    {
                        _connectionStatus.OnNext(default);
                        if (_reconnectCancellation != null && !_reconnectCancellation.IsCancellationRequested)
                        {
                            StartReconnectLoop();
                        }
                    }
                );
        }

        public async Task StartAutoReconnect()
        {
            _reconnectCancellation?.Cancel();
            _reconnectCancellation = new CancellationTokenSource();

            var connected = await ConnectAsync();
            
            if (!connected && _reconnectCancellation != null && !_reconnectCancellation.IsCancellationRequested)
            {
                StartReconnectLoop();
            }
        }

        public void StopAutoReconnect()
        {
            _reconnectCancellation?.Cancel();
            _reconnectTask = null;
        }

        private void StartReconnectLoop()
        {
            if (_reconnectTask != null && !_reconnectTask.IsCompleted)
                return;

            _reconnectTask = Task.Run(
                async () =>
                {
                    while (_reconnectCancellation?.IsCancellationRequested == false)
                    {
                        try
                        {
                            await Task.Delay(3000, _reconnectCancellation.Token);

                            if (_reconnectCancellation.IsCancellationRequested)
                                break;

                            var connected = await ConnectAsync();

                            if (connected && IsConnected)
                            {
                                break;
                            }
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception)
                        {
                        }
                    }
                },
                _reconnectCancellation?.Token ?? CancellationToken.None
            );
        }

        public IObservable<T> ObserveAddress<T>(
            string address,
            TransmissionMode mode = TransmissionMode.OnChange
        )
            where T : struct
        {
            return ConnectionStatus
                .Where(state => state == ConnectionState.Connected)
                .SelectMany(_ =>
                {
                    if (Plc == null)
                        return Observable.Empty<T>();

                    return Plc.CreateNotification<T>(address, mode)
                        .Catch<T, Exception>(ex =>
                        {
                            return Observable.Return(default(T));
                        });
                });
        }

        public void Disconnect()
        {
            StopAutoReconnect();
            CleanupConnection();
            _connectionStatus.OnNext(default);
        }

        private void CleanupConnection()
        {
            _connectionStateSubscription?.Dispose();
            _connectionStateSubscription = null;

            Plc?.Dispose();
            Plc = null;
        }

        public void Dispose()
        {
            Disconnect();
            _reconnectCancellation?.Dispose();
            _connectionStatus?.Dispose();
            _connectionLock?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
