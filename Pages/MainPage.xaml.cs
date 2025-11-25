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
        private bool _isConnected = false;
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
                var connectionSubscription = _plcService.ConnectionStatus
                    .DistinctUntilChanged()
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        state =>
                        {
                            if (state == ConnectionState.Connected)
                            {
                                IsConnected = true;
                                Console.WriteLine("✅ PLC conectado!");
                            }
                            else
                            {
                                IsConnected = false;
                                Console.WriteLine("❌ PLC desconectado!");
                            }
                        },
                        error =>
                        {
                            Console.WriteLine($"❌ Erro ao monitorar status: {error.Message}");
                            IsConnected = false;
                        });

                _disposables.Add(connectionSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao subscrever status de conexão: {ex.Message}");
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}