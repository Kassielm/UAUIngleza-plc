using Sharp7;
using Sharp7.Rx;
using System.Diagnostics;
using UAUIngleza_plc.Models;

namespace UAUIngleza_plc.Services.Plc
{
    public interface IPLCService
    {
        Task<bool> ConnectAsync(PlcConfiguration config);
        bool IsConnected { get; }
    }

    public class PLCService : IPLCService
    {
        private S7Client _client;
        public bool IsConnected => _client?.Connected ?? false;

        public PLCService()
        {
            _client = new S7Client();
        }

        public async Task<bool> ConnectAsync(PlcConfiguration config)
        {
            try
            {
                int result = _client.ConnectTo(
                    config.IpAddress,
                    config.Rack,
                    config.Slot
                );

                bool success = result == 0;

                if (success)
                    Debug.WriteLine("Conectado ao PLC com sucesso!");
                else
                    Debug.WriteLine($"Erro ao conectar: código {result}");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exceção ao conectar: {ex.Message}");
                return false;
            }
        }
    }
}