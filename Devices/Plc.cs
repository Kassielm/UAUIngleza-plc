using Sharp7;
using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using UAUIngleza_plc.Settings;

namespace UAUIngleza_plc.Devices.Plc
{
    public class Plc
    {
        private readonly Sharp7Plc Client;
        private readonly string Ip = SPlc.Default.Ip;
        private readonly int Rack = SPlc.Default.Rack;
        private readonly int Slot = SPlc.Default.Slot;

        public Plc()
        {
            Client = new Sharp7Plc(Ip, Rack, Slot);
            Task.Run(async () =>
            {
                await InitializePlc();
            });
        }

        public async Task<bool> InitializePlc()
        {
            try
            {
                return await Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao conectar com o PLC: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> Connect()
        {
            try
            {
                Console.WriteLine($"🔄 Conectando ao PLC {Ip}...");
                
                await Client.InitializeConnection();
                
                // Aguarda o estado de conexão usando Observable (melhor que Thread.Sleep)
                var isConnected = await WaitForConnectionStatus(TimeSpan.FromSeconds(5));
                
                if (isConnected)
                {
                    Console.WriteLine("✅ Conectado ao PLC com sucesso!");
                    SubscribeToConnectionChanges();
                }
                else
                {
                    Console.WriteLine("❌ Timeout ao aguardar conexão com o PLC");
                }
                
                return isConnected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao conectar com o PLC: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aguarda até que o PLC esteja conectado ou timeout
        /// </summary>
        private async Task<bool> WaitForConnectionStatus(TimeSpan timeout)
        {
            try
            {
                var state = await Client.ConnectionState
                    .Where(s => s == ConnectionState.Connected)
                    .Timeout(timeout)
                    .FirstAsync();
                
                return state == ConnectionState.Connected;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⏱️ Timeout ao aguardar conexão");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao verificar status: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retorna o status atual da conexão de forma síncrona
        /// </summary>
        public async Task<bool> GetPlcStatus()
        {
            try
            {
                var currentState = await Client.ConnectionState
                    .FirstAsync()
                    .Timeout(TimeSpan.FromSeconds(2));
                
                return currentState == ConnectionState.Connected;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao obter status do PLC: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Subscreve às mudanças de estado de conexão para monitoramento contínuo
        /// </summary>
        private void SubscribeToConnectionChanges()
        {
            try
            {
                _connectionSubscription?.Dispose();
                
                _connectionSubscription = Client.ConnectionState
                    .DistinctUntilChanged()
                    .Subscribe(
                        state =>
                        {
                            if (state == ConnectionState.Connected)
                            {
                                Console.WriteLine("🟢 PLC CONECTADO");
                            }
                            else
                            {
                                Console.WriteLine("🔴 PLC DESCONECTADO");
                            }
                        },
                        error =>
                        {
                            Console.WriteLine($"❌ Erro no monitoramento de conexão: {error.Message}");
                        });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao subscrever mudanças de conexão: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica se o PLC está conectado (método antigo mantido para compatibilidade)
        /// </summary>
        [Obsolete("Use GetPlcStatus() ou WaitForConnectionStatus() ao invés disso")]
        public async void CheckConnection()
        {
            try
            {
                var state = await Client.ConnectionState
                    .Where(s => s == ConnectionState.Connected)
                    .Timeout(TimeSpan.FromSeconds(5))
                    .FirstAsync();
                
                Console.WriteLine("✅ Conectado ao PLC");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⏱️ Timeout ao verificar conexão");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao verificar conexão: {ex.Message}");
            }
        }

        public IDisposable? SubscribeAddress<T>(string address, Action<T> callback)
        {
            try
            {
                return Client
                    .CreateNotification<T>(address, TransmissionMode.OnChange)
                    .Subscribe(callback);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao se inscrever no endereço {address}: {ex.Message}");
                return null;
            }
        }

        public async Task HandleFloats(string tag, float value)
        {
            try
            {
                await Client.SetValue(tag, value);
                Console.WriteLine($"✅ Float escrito em {tag}: {value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao escrever float em {tag}: {ex.Message}");
                throw;
            }
        }

        public async Task HandleNumbers(string tag, int value)
        {
            try
            {
                if (tag.Contains("DINT"))
                {
                    await Client.SetValue(tag, value * 1000);
                    Console.WriteLine($"✅ DINT escrito em {tag}: {value * 1000}");
                }
                else
                {
                    await Client.SetValue(tag, value);
                    Console.WriteLine($"✅ INT escrito em {tag}: {value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao escrever número em {tag}: {ex.Message}");
                throw;
            }
        }

        public async Task<object> ReadFromPlc(string tag)
        {
            try
            {
                var value = await Client.GetValue(tag);
                Console.WriteLine($"📖 Lido de {tag}: {value}");
                return value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao ler a tag {tag}: {ex.Message}");
                throw new Exception($"Erro ao ler a tag {tag} do PLC. " + ex.Message);
            }
        }

        public async Task WriteToPlc(string tag, string value)
        {
            try
            {
                await HandleValue(tag, value);
                Console.WriteLine($"✅ Valor '{value}' escrito em {tag}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao escrever em {tag}: {ex.Message}");
                throw new Exception($"Erro ao escrever a tag {tag} no PLC. " + ex.Message);
            }
        }

        private async Task HandleValue(string tag, string value)
        {
            value = value.Replace(".", ",");

            if (tag.Contains("DINT") || tag.Contains("INT"))
            {
                await HandleNumbers(tag, int.Parse(value));
            }
            else if (Regex.IsMatch(tag, @"D[0-9]"))
            {
                await HandleFloats(tag, (float)double.Parse(value));
            }
            else if (tag.Contains("STRING"))
            {
                await Client.SetValue(tag, value);
            }
            else if (tag.Contains("DBX"))
            {
                await Client.SetValue(tag, bool.Parse(value));
            }
            else if (tag.Contains("BYTE"))
            {
                await Client.SetValue(tag, byte.Parse(value));
            }
            else
            {
                throw new Exception($"Tipo de tag não suportado: {tag}");
            }
        }

        /// <summary>
        /// Desconecta e limpa recursos
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _connectionSubscription?.Dispose();
                Client?.Dispose();
                Console.WriteLine("🔌 Desconectado do PLC");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao desconectar: {ex.Message}");
            }
        }
    }
}
