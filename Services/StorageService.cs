using System.Diagnostics;
using System.Text.Json;

namespace UAUIngleza_plc.Services
{
    public interface IStorageService
    {
        Task SaveConfigAsync(Models.SystemConfiguration config);
        Task<Models.SystemConfiguration> GetConfigAsync();
        Task SaveRecipesAsync(Models.RecipesConfiguration recipes);
        Task<Models.RecipesConfiguration> GetRecipesAsync();
    }

    public class StorageService : IStorageService
    {
        private const string ConfigKey = "plcConfiguration";
        private const string RecipesKey = "recipesConfiguration";

        public async Task SaveConfigAsync(Models.SystemConfiguration config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config);
                await SecureStorage.Default.SetAsync(ConfigKey, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao salvar configuração: {ex.Message}");
            }
        }
        
        public async Task<Models.SystemConfiguration> GetConfigAsync()
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync(ConfigKey);
                if (string.IsNullOrEmpty(json))
                {
                    return new Models.SystemConfiguration();
                }
                return JsonSerializer.Deserialize<Models.SystemConfiguration>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar configuração: {ex.Message}");
                return new Models.SystemConfiguration();
            }
        }

        public async Task SaveRecipesAsync(Models.RecipesConfiguration recipes)
        {
            try
            {
                var json = JsonSerializer.Serialize(recipes);
                await SecureStorage.Default.SetAsync(RecipesKey, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao salvar receitas: {ex.Message}");
            }
        }

        public async Task<Models.RecipesConfiguration> GetRecipesAsync()
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync(RecipesKey);
                if (string.IsNullOrEmpty(json))
                {
                    return new Models.RecipesConfiguration();
                }
                return JsonSerializer.Deserialize<Models.RecipesConfiguration>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao buscar receitas: {ex.Message}");
                return new Models.RecipesConfiguration();
            }
        }
    }
}
