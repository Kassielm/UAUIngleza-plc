using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Services;
using SystemConfig = UAUIngleza_plc.Models.SystemConfiguration;

namespace UAUIngleza_plc.Repositories
{
    public class ConfigRepository(DatabaseService databaseService) : IConfigRepository
    {
        private readonly DatabaseService _databaseService = databaseService;

        public async Task<List<T>> GetAsync<T>()
            where T : class
        {
            if (typeof(T) == typeof(SystemConfig))
            {
                await _databaseService.InitAsync();
                var config = await _databaseService.GetConfigurationAsync();
                return
                [
                    config as T
                        ?? throw new InvalidOperationException("Configuração não encontrada"),
                ];
            }
            throw new NotSupportedException(
                $"Tipo {typeof(T).Name} não suportado por ConfigRepository."
            );
        }

        public async Task<T?> GetOneAsync<T>(int id)
            where T : class
        {
            if (typeof(T) == typeof(SystemConfig))
            {
                await _databaseService.InitAsync();
                var config = await _databaseService.GetConfigurationAsync();
                return config as T;
            }
            throw new NotSupportedException(
                $"Tipo {typeof(T).Name} não suportado por ConfigRepository."
            );
        }

        public async Task SaveAsync<T>(T type)
            where T : class
        {
            if (type is SystemConfig config)
            {
                await _databaseService.InitAsync();
                await _databaseService.SaveConfigurationAsync(config);
                return;
            }
            throw new NotSupportedException(
                $"Tipo {typeof(T).Name} não suportado por ConfigRepository."
            );
        }

        public async Task UpdateAsync<T>(T type)
            where T : class
        {
            if (type is SystemConfig config)
            {
                await _databaseService.InitAsync();
                await _databaseService.SaveConfigurationAsync(config);
                return;
            }
            throw new NotSupportedException(
                $"Tipo {typeof(T).Name} não suportado por ConfigRepository."
            );
        }

        public async Task DeleteAsync<T>(T type)
            where T : class
        {
            throw new NotSupportedException("Não é possível deletar a configuração do sistema.");
        }
    }
}
