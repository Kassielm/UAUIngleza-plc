using System.Diagnostics;
using System.Text.Json;

namespace UAUIngleza_plc.Services
{
    public interface IStorageService
    {
        Task SaveConfigAsync(Models.SystemConfiguration config);
        Task<Models.SystemConfiguration> GetConfigAsync();
    }

    public class StorageService : IStorageService
    {
        private const string ConfigKey = "plcConfiguration";

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
    }
}
