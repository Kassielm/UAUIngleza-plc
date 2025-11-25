using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IPLCService _plcService;
        private string _connectionStatus = "Verificando conexão...";
        private string _bitValue = "---";
        private bool _isConnected = false;
        private bool _isSettingBit = false;

        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string BitValue
        {
            get => _bitValue;
            set
            {
                if (_bitValue != value)
                {
                    _bitValue = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSettingBit
        {
            get => _isSettingBit;
            set
            {
                if (_isSettingBit != value)
                {
                    _isSettingBit = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainPage(IPLCService plcService, IStorageService storageService)
        {
            InitializeComponent();
            _plcService = plcService;
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SubscribeToConnectionStatus();
            SubscribeToBitChanges();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _disposables.Clear();
        }

        private void SubscribeToConnectionStatus()
        {
            try
            {
                var connectionSubscription = _plcService.ConnectionStatus
                    .DistinctUntilChanged()
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        state =>
                        {
                            if (state == ConnectionState.Connected)
                            {
                                ConnectionStatus = "🟢 PLC ONLINE";
                                IsConnected = true;
                                Console.WriteLine("✅ PLC conectado!");
                            }
                            else
                            {
                                ConnectionStatus = "🔴 PLC OFFLINE";
                                IsConnected = false;
                                BitValue = "---";
                                Console.WriteLine("❌ PLC desconectado!");
                            }
                        },
                        error =>
                        {
                            Console.WriteLine($"❌ Erro ao monitorar status: {error.Message}");
                            ConnectionStatus = "⚠️ ERRO NO STATUS";
                            IsConnected = false;
                        });

                _disposables.Add(connectionSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao subscrever status de conexão: {ex.Message}");
            }
        }

        private void SubscribeToBitChanges()
        {
            try
            {
                var bitSubscription = _plcService.ConnectionStatus
                    .Where(state => state == ConnectionState.Connected)
                    .SelectMany(_ =>
                    {
                        if (_plcService.Plc == null)
                            return Observable.Empty<short>();

                        return _plcService.Plc.CreateNotification<short>("DB1.DBW0", TransmissionMode.OnChange)
                            .Catch<short, Exception>(ex =>
                            {
                                Console.WriteLine($"⚠️ Erro ao ler bit: {ex.Message}");
                                return Observable.Return<short>(0);
                            });
                    })
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        value =>
                        {
                            BitValue = value.ToString();
                            Console.WriteLine($"📊 Valor do bit alterado: {value}");
                        },
                        error =>
                        {
                            Console.WriteLine($"❌ Erro na notificação do bit: {error.Message}");
                            BitValue = "ERRO";
                        });

                _disposables.Add(bitSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao subscrever mudanças do bit: {ex.Message}");
            }
        }

        private async void OnSetBitClicked(object? sender, EventArgs e)
        {
            if (!IsConnected)
            {
                await DisplayAlert("Erro", "PLC não está conectado!", "OK");
                return;
            }

            IsSettingBit = true;

            try
            {
                Console.WriteLine("⬆️ Setando bit para 1...");
                
                await _plcService.Plc!.SetValue<short>("DB1.DBW0", 1);
                
                Console.WriteLine("✅ Bit setado com sucesso!");
                await DisplayAlert("Sucesso", "Bit setado para 1", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao setar bit: {ex.Message}");
                await DisplayAlert("Erro", $"Erro ao setar bit: {ex.Message}", "OK");
            }
            finally
            {
                IsSettingBit = false;
            }
        }

        private async void OnResetBitClicked(object? sender, EventArgs e)
        {
            if (!IsConnected)
            {
                await DisplayAlert("Erro", "PLC não está conectado!", "OK");
                return;
            }

            IsSettingBit = true;

            try
            {
                Console.WriteLine("⬇️ Resetando bit para 0...");
                
                await _plcService.Plc!.SetValue<short>("DB1.DBW0", 0);
                
                Console.WriteLine("✅ Bit resetado com sucesso!");
                await DisplayAlert("Sucesso", "Bit resetado para 0", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao resetar bit: {ex.Message}");
                await DisplayAlert("Erro", $"Erro ao resetar bit: {ex.Message}", "OK");
            }
            finally
            {
                IsSettingBit = false;
            }
        }

        private void OnMenuClicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
