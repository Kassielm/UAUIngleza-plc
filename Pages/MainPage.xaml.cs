using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;
using UAUIngleza_plc.Settings;


namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IPLCService _plcService;
        private string _connectionStatus = "🔄 Verificando conexão...";
        private string _bitValue = "---";
        private bool _isConnected = false;
        private bool _isProcessing = false;

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
                    OnPropertyChanged(nameof(CanInteract));
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanInteract));
                }
            }
        }

        public bool CanInteract => IsConnected && !IsProcessing;

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
                // Observable reativo que escuta mudanças no status de conexão
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
                                Console.WriteLine("✅ MainPage: PLC conectado!");
                            }
                            else
                            {
                                ConnectionStatus = "🔴 PLC OFFLINE";
                                IsConnected = false;
                                BitValue = "---";
                                Console.WriteLine("❌ MainPage: PLC desconectado!");
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