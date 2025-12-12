using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class App : Application
    {
        private readonly IPlcService _plcService;

        public App(IPlcService plcService, IStorageService storageService)
        {
            InitializeComponent();
            _plcService = plcService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Inicia a conexão em background sem bloquear a UI
            _ = Task.Run(async () =>
            {
                try
                {
                    await _plcService.StartAutoReconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao iniciar conexão automática: {ex.Message}");
                }
            });
        }

        protected override void OnSleep()
        {
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!_plcService.IsConnected)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _plcService.ConnectAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao reconectar após resume: {ex.Message}");
                    }
                });
            }
        }
    }
}
