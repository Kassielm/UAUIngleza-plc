using Microsoft.Extensions.Logging;
using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Pages;
using UAUIngleza_plc.Repositories;
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
                    fonts.AddFont("MaterialIconsRound-Regular.otf", "MaterialIcons");
                });

            builder.Services.AddSingleton<DatabaseService>();
            builder.Services.AddSingleton<IPlcService, PLCService>();
            builder.Services.AddSingleton<IRecipeRepository, RecipeRepository>();
            builder.Services.AddSingleton<IConfigRepository, ConfigRepository>();

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<ConfigPage>();
            builder.Services.AddSingleton<ReceitasPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
