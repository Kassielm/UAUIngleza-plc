using Microsoft.AspNetCore.Components;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UAUIngleza_plc.Configuration;
using UAUIngleza_plc.Settings;

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
        Task<bool> WriteValue<T>(string address, T value);
        Task<T?> ReadValue<T>(string address);
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
                // Tenta obter configuração do localStorage
                var config = await _storageService.GetConfigAsync();

                string ip;
                int rack;
                int slot;

                // Se não existir no localStorage, usa valores padrão
                if (config == null || string.IsNullOrEmpty(config.IpAddress))
                {
                    ip = SPlc.Default.Ip;
                    rack = SPlc.Default.Rack;
                    slot = SPlc.Default.Slot;
                    Console.WriteLine($"⚙️ Usando configuração padrão: IP={ip}, Rack={rack}, Slot={slot}");
                }
                else
                {
                    ip = config.IpAddress;
                    rack = config.Rack;
                    slot = config.Slot;
                    Console.WriteLine($"⚙️ Usando configuração do localStorage: IP={ip}, Rack={rack}, Slot={slot}");
                }

                // Limpa conexão anterior se existir
                CleanupConnection();

                Console.WriteLine($"🔄 Tentando conectar em: {ip} Rack: {rack} Slot: {slot}");

                Plc = new Sharp7Plc(ip, rack, slot);

                // Timeout de 5 segundos para não travar a aplicação
                var connectionTask = Plc.InitializeConnection();
                var timeoutTask = Task.Delay(5000);

                var completedTask = await Task.WhenAny(connectionTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("⏱️ Timeout ao conectar ao PLC");
                    _connectionStatus.OnNext(default(ConnectionState));
                    CleanupConnection();
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

                        // Se desconectar e auto-reconnect estiver ativo, tenta reconectar
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

            // Tenta conectar inicialmente
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

        public async Task<bool> WriteValue<T>(string address, T value)
        {
            if (Plc == null || !IsConnected)
            {
                Console.WriteLine($"❌ Não é possível escrever em {address}: PLC não conectado");
                return false;
            }

            try
            {
                await Plc.SetValue(address, value);
                Console.WriteLine($"✅ Valor escrito em {address}: {value}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao escrever em {address}: {ex.Message}");
                return false;
            }
        }

        public async Task<T?> ReadValue<T>(string address)
        {
            if (Plc == null || !IsConnected)
            {
                Console.WriteLine($"❌ Não é possível ler de {address}: PLC não conectado");
                return default;
            }

            try
            {
                var value = await Plc.GetValue<T>(address);
                Console.WriteLine($"📖 Valor lido de {address}: {value}");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao ler de {address}: {ex.Message}");
                return default;
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
