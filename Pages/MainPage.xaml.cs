using System.Reactive.Disposables;
using UAUIngleza_plc.Devices.Plc;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Plc _plc;
        private readonly PlcService _plcService;
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
        public MainPage()
        {
            InitializeComponent();
            _plc = new Plc();
            _plcService = new PlcService(_plc);

            InitializePlc();
        }

        private void InitializePlc()
        {
            Task.Run(async () =>
            {
                if (!await ConnectPlc())
                    Console.WriteLine("Erro ao conectar com o PLC.");
            })
                .ContinueWith(async t =>
                {
                    if (t.IsCompleted)
                        if (await IsPlcConnected())
                            Console.WriteLine("✅ PLC conectado!");
                        else
                            Console.WriteLine("❌ PLC desconectado!");
                });
        }

        private async Task<bool> ConnectPlc() => await _plcService.Connect();
        private async Task<bool> IsPlcConnected() => await _plc.CheckConnection();

        //    protected override void OnAppearing()
        //    {
        //        base.OnAppearing();
        //        SubscribeToConnectionStatus();
        //    }

        //    protected override void OnDisappearing()
        //    {
        //        base.OnDisappearing();
        //        _disposables.Clear();
        //    }

        //    private void SubscribeToConnectionStatus()
        //    {
        //        try
        //        {
        //            var connectionSubscription = _plcService.ConnectionStatus
        //                .DistinctUntilChanged()
        //                .ObserveOn(SynchronizationContext.Current!)
        //                .Subscribe(
        //                    state =>
        //                    {
        //                        if (state == ConnectionState.Connected)
        //                        {
        //                            IsConnected = true;
        //                            Console.WriteLine("✅ PLC conectado!");
        //                        }
        //                        else
        //                        {
        //                            IsConnected = false;
        //                            Console.WriteLine("❌ PLC desconectado!");
        //                        }
        //                    },
        //                    error =>
        //                    {
        //                        Console.WriteLine($"❌ Erro ao monitorar status: {error.Message}");
        //                        IsConnected = false;
        //                    });

        //            _disposables.Add(connectionSubscription);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"❌ Erro ao subscrever status de conexão: {ex.Message}");
        //        }
        //    }

        //    public new event PropertyChangedEventHandler? PropertyChanged;

        //    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        //    {
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        //    }
        //}
    //}