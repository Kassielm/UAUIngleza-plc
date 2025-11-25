using Microsoft.AspNetCore.Components;
using Sharp7.Rx;
using System.Diagnostics;

namespace UAUIngleza_plc.Services
{
    public interface IPLCService
    {
        Sharp7Plc Plc { get; }
        Task ConnectAsync();
        void Disconnect();
    }
    public class PLCService : IPLCService
    {
        private readonly IStorageService _storageService;

        public Sharp7Plc Plc { get; private set; }

        public PLCService(IStorageService storageService)
        {
            _storageService = storageService;

            // usa endereço padrão para evitar erros
            Plc = new Sharp7Plc("127.0.0.1", 0, 0);
        }

        public async Task ConnectAsync()
        {
            try
            {
                var config = await _storageService.GetConfigAsync();

                if (config != null && !string.IsNullOrEmpty(config.IpAddress))
                {
                    Plc?.Dispose();

                    Console.WriteLine($"Tentando conectar em: {config.IpAddress} Rack: {config.Rack} Slot: {config.Slot}");

                    Plc = new Sharp7Plc(config.IpAddress, config.Rack, config.Slot);

                    await Plc.InitializeConnection();

                    Console.WriteLine("✅ Conexão Inicializada!");
                }
                else
                {
                    Console.WriteLine("⚠️ Nenhuma configuração encontrada no Storage.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exceção ao conectar PLC: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            Plc?.Dispose();
        }
    }
}