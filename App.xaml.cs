using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class App : Application
    {
        [Inject] private PLCService _plcService { get; set; } = default!;
        public App(IPLCService plcService, IStorageService storageService)
        {
            InitializeComponent();
        }

        protected override async void OnStart()
        {
            base.OnStart();
            _plcService.ConnectAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        private void CheckStatus()
        {
            try
            {
                _plcService.
            }
        }
    }
}