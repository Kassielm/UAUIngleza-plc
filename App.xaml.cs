using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class App : Application
    {
        private IPLCService _plcService;

        public App(IPLCService plcService, IStorageService storageService)
        {
            InitializeComponent();
            _plcService = plcService;
        }

        protected override async void OnStart()
        {
            base.OnStart();
            try
            {
                _plcService.ConnectAsync().Wait(3000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("deu ruim", ex);
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}