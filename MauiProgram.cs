using Microsoft.Extensions.Logging;
using UAUIngleza_plc.Pages;
using UAUIngleza_plc.Services;
using UAUIngleza_plc.ViewModels;

namespace UAUIngleza_plc
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<IStorageService, StorageService>();
            builder.Services.AddSingleton<IPLCService, PLCService>();

            builder.Services.AddSingleton<ConfiguracoesViewModel>();

            // Registrar Page
            builder.Services.AddSingleton<ConfiguracoesPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
