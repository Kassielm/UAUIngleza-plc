using System.Diagnostics;
using System.Text.Json;
using UAUIngleza_plc.Models;

namespace UAUIngleza_plc.Services
{
    public interface IStorageService
    {
        Task SaveConfigAsync(PlcConfiguration config);
        Task<PlcConfiguration> GetConfigAsync();
    }

    public class StorageService : IStorageService
    {
        private const string ConfigKey = "plcConfiguration";

        public async Task SaveConfigAsync(PlcConfiguration config)
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
        
        public async Task<PlcConfiguration> GetConfigAsync()
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync(ConfigKey);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }
                  return JsonSerializer.Deserialize<PlcConfiguration>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving configuration: {ex.Message}");
                return null;
            }
        }
    }
}
