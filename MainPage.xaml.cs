using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        private IPLCService _plcService;
        int count = 0;
        short plcteste = 0;

        public MainPage(IPLCService plcService, IStorageService storageService)
        {
            InitializeComponent();
            _plcService = plcService;
            InitializeConnetion();
        }

        private void OnCounterClicked(object? sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        public Sharp7Plc plc = new Sharp7Plc("192.168.0.1", 0, 2);

        public void InitializeConnetion()
        {
            plc.InitializeConnection().Wait(3000);
        }

        public async Task Setbit()
        {
            try
            {
                Console.WriteLine("chegou");
                await plc.SetValue<short>("Db1.Int0", 1);
                plcteste = await plc.GetValue<short>("Db1.Int0");
                Console.WriteLine("Deu certo");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception("Erro ao setar bit. " + ex.Message);
            }
        }

        public async Task Resebit()
        {
            try
            {
                await plc.SetValue<short>("Db1.Int0", 0);
                plcteste = await plc.GetValue<short>("Db1.Int0");
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao resetar bit. " + ex.Message);
            }
        }

        public async Task WriteInt(string address, short value)
        {
            try
            {
                await plc.SetValue<short>(address, value);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao esrever no inteiro. " + ex.Message);
            }
        }

        public async Task<short> GetInt(string address)
        {
            try
            {
                return await plc.GetValue<short>(address);
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao ler inteiro. " + ex.Message);
            }
        }

        private void OnMenuClicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }
    protected override void OnAppearing()
        {
            base.OnAppearing();
            CheckStatus();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _disposables.Clear();
        }

        public void CheckStatus()
        {
            try
            {
                if (_plcService.Plc == null) return;

                var escrita = _plcService.Plc.CreateNotification<short>("Db1.Int0", TransmissionMode.OnChange)
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(value =>
                    {
                        Bit.Text = value.ToString();
                    });

                _disposables.Add(escrita);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao subscrever tags: {ex.Message}");
            }
        }
    }
}
