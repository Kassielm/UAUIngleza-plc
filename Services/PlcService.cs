using UAUIngleza_plc.Devices.Plc;
using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Settings;
using System.Diagnostics;

namespace UAUIngleza_plc.Services
{
    public class PlcService(Plc plc) : IPlcService
    {
        private readonly Plc _plc = plc;

        public async Task<bool> Connect() => await _plc.Connect();

        public async Task<bool> EnsureConnection() => await _plc.CheckConnection();

        public async Task WriteToPlc(int receita, string? address, string? value)
        {
            switch (receita)
            {
                case 1:
                    await _plc.WriteToPlc(SPlcAddresses.Default.Receita1, "true");
                    break;
                case 2:
                    await _plc.WriteToPlc(SPlcAddresses.Default.Receita2, "true");
                    break;
                case 3:
                    await _plc.WriteToPlc(SPlcAddresses.Default.Receita3, "true");
                    break;
                case 4:
                    await _plc.WriteToPlc(SPlcAddresses.Default.Receita4, "true");
                    break;
                case 5:
                    await _plc.WriteToPlc(SPlcAddresses.Default.Receita5, "true");
                    break;
                default:
                    Debug.WriteLine("Receita not found.");
                    break;
            }
        }
    }
}
