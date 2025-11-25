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
                Debug.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }
        
        public async Task<Models.SystemConfiguration> GetConfigAsync()
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync(ConfigKey);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }
                return JsonSerializer.Deserialize<Models.SystemConfiguration>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving configuration: {ex.Message}");
                return null;
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
                Debug.WriteLine($"Error saving recipes: {ex.Message}");
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
                Debug.WriteLine($"Error retrieving recipes: {ex.Message}");
                return new Models.RecipesConfiguration();
            }
        }
    }
}
