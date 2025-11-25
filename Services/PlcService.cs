using Microsoft.AspNetCore.Components;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace UAUIngleza_plc.Services
{
    public interface IPLCService
    {
        Sharp7Plc? Plc { get; }
        IObservable<ConnectionState> ConnectionStatus { get; }
        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        void Disconnect();
        Task StartAutoReconnect();
        void StopAutoReconnect();
    }

    public class PLCService : IPLCService, IDisposable
    {
        private readonly IStorageService _storageService;
        private readonly BehaviorSubject<ConnectionState> _connectionStatus;
        private CancellationTokenSource? _reconnectCancellation;
        private IDisposable? _connectionStateSubscription;
        private bool _isReconnecting = false;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

        public Sharp7Plc? Plc { get; private set; }
        public IObservable<ConnectionState> ConnectionStatus => _connectionStatus.AsObservable();
        public bool IsConnected => _connectionStatus.Value == ConnectionState.Connected;

        public PLCService(IStorageService storageService)
        {
            _storageService = storageService;
            _connectionStatus = new BehaviorSubject<ConnectionState>(default(ConnectionState));
        }

        public async Task<bool> ConnectAsync()
        {
            await _connectionLock.WaitAsync();
            try
            {
                var config = await _storageService.GetConfigAsync();

                if (config == null || string.IsNullOrEmpty(config.IpAddress))
                {
                    Console.WriteLine("⚠️ Nenhuma configuração encontrada no Storage.");
                    _connectionStatus.OnNext(default(ConnectionState));
                    return false;
                }

                CleanupConnection();

                Console.WriteLine($"🔄 Tentando conectar em: {config.IpAddress} Rack: {config.Rack} Slot: {config.Slot}");

                Plc = new Sharp7Plc(config.IpAddress, config.Rack, config.Slot);

                var connectionTask = Plc.InitializeConnection();
                var timeoutTask = Task.Delay(5000);

                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("⏱️ Timeout ao conectar ao PLC");
                    _connectionStatus.OnNext(default(ConnectionState));
                    CleanupConnection();
                    Console.WriteLine("❌ Falha na conexão!");
                    return false;
                }

                // Subscreve ao estado de conexão do PLC
                SubscribeToConnectionState();

                Console.WriteLine("✅ Conexão Inicializada!");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exceção ao conectar PLC: {ex.Message}");
                _connectionStatus.OnNext(default(ConnectionState));
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
            if (Plc == null) return;

            _connectionStateSubscription?.Dispose();

            _connectionStateSubscription = Plc.ConnectionState
                .DistinctUntilChanged()
                .Subscribe(
                    state =>
                    {
                        Console.WriteLine($"🔌 Estado da conexão: {state}");
                        _connectionStatus.OnNext(state);

                        if (state != ConnectionState.Connected && _reconnectCancellation != null && !_reconnectCancellation.IsCancellationRequested)
                        {
                            _ = TryReconnect();
                        }
                    },
                    error =>
                    {
                        Console.WriteLine($"❌ Erro no stream de conexão: {error.Message}");
                        _connectionStatus.OnNext(default(ConnectionState));
                    });
        }

        public async Task StartAutoReconnect()
        {
            _reconnectCancellation?.Cancel();
            _reconnectCancellation = new CancellationTokenSource();

            var connected = await ConnectAsync();
            
            if (!connected)
            {
                Console.WriteLine("⚠️ Falha na conexão inicial, mas continuando com a aplicação...");
            }
        }

        public void StopAutoReconnect()
        {
            _reconnectCancellation?.Cancel();
            _isReconnecting = false;
        }

        private async Task TryReconnect()
        {
            if (_isReconnecting || _reconnectCancellation?.IsCancellationRequested == true)
                return;

            _isReconnecting = true;

            try
            {
                Console.WriteLine("🔄 Tentando reconectar ao PLC...");
                
                await Task.Delay(3000, _reconnectCancellation?.Token ?? CancellationToken.None);

                if (_reconnectCancellation?.IsCancellationRequested == true)
                    return;

                var connected = await ConnectAsync();

                if (connected)
                {
                    Console.WriteLine("✅ Reconexão bem-sucedida!");
                }
                else
                {
                    Console.WriteLine("❌ Falha na reconexão. Nova tentativa em 5 segundos...");
                    await Task.Delay(5000, _reconnectCancellation?.Token ?? CancellationToken.None);
                    
                    if (_reconnectCancellation?.IsCancellationRequested == false)
                    {
                        _ = TryReconnect();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("⏹️ Reconexão cancelada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro durante reconexão: {ex.Message}");
            }
            finally
            {
                _isReconnecting = false;
            }
        }

        public async Task SetIntBit(string address, short value)
        {
            try
            {
                await Plc.SetValue<short>(address, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao definir valor INT no PLC: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            StopAutoReconnect();
            CleanupConnection();
            _connectionStatus.OnNext(default(ConnectionState));
            Console.WriteLine("🔌 Desconectado do PLC");
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
        }
    }
}