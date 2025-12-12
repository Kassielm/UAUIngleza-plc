using Microsoft.Extensions.Logging;
using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Pages;
using UAUIngleza_plc.Services;

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
            builder.Services.AddSingleton<IPlcService, PLCService>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ConfiguracoesPage>();
            builder.Services.AddSingleton<ReceitasPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
