using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;
using UAUIngleza_plc.Services.Plc;

namespace UAUIngleza_plc.Pages
{
    public class PLCConfigurationViewModel : INotifyPropertyChanged
    {
        private readonly IStorageService _storageService;
        private readonly IPLCService _plcService;

        private string _ipAddress;
        private string _rack;
        private string _slot;
        private string _statusMessage;
        private bool _isConnecting;
        private bool _isConnected;

        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Rack
        {
            get => _rack;
            set
            {
                if (_rack != value)
                {
                    _rack = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Slot
        {
            get => _slot;
            set
            {
                if (_slot != value)
                {
                    _slot = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnecting
        {
            get => _isConnecting;
            set
            {
                if (_isConnecting != value)
                {
                    _isConnecting = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand SaveConfigCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public PLCConfigurationViewModel(IStorageService storageService, IPLCService plcService)
        {
            _storageService = storageService;
            _plcService = plcService;

            SaveConfigCommand = new Command(async () => await SaveConfiguration());
            ConnectCommand = new Command(async () => await Connect());

            LoadConfiguration();
        }

        private async void LoadConfiguration()
        {
            var config = await _storageService.GetConfigAsync();
            if (config != null)
            {
                IpAddress = config.IpAddress;
                Rack = config.Rack.ToString();
                Slot = config.Slot.ToString();
                StatusMessage = "Configuração carregada";
            }
        }

        private async Task SaveConfiguration()
        {
            try
            {
                if (!ValidateInputs())
                {
                    StatusMessage = "IP, Rack e Slot são obrigatórios";
                    return;
                }

                var config = new PlcConfiguration
                {
                    IpAddress = IpAddress,
                    Rack = int.Parse(Rack),
                    Slot = int.Parse(Slot)
                };

                await _storageService.SaveConfigAsync(config);
                StatusMessage = "Configuração salva com sucesso!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Erro ao salvar: {ex.Message}";
            }
        }

        private async Task Connect()
        {
            try
            {
                if (!ValidateInputs())
                {
                    StatusMessage = "Preencha todos os campos antes de conectar";
                    return;
                }

                IsConnecting = true;
                StatusMessage = "Conectando...";

                var config = new PlcConfiguration
                {
                    IpAddress = IpAddress,
                    Rack = int.Parse(Rack),
                    Slot = int.Parse(Slot)
                };

                bool success = await _plcService.ConnectAsync(config);

                if (success)
                {
                    IsConnected = true;
                    StatusMessage = "Conectado ao PLC!";
                }
                else
                {
                    IsConnected = false;
                    StatusMessage = "Falha ao conectar ao PLC";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = $"Erro: {ex.Message}";
            }
            finally
            {
                IsConnecting = false;
            }
        }

        private bool ValidateInputs()
        {
            return !string.IsNullOrWhiteSpace(IpAddress) &&
                   !string.IsNullOrWhiteSpace(Rack) &&
                   !string.IsNullOrWhiteSpace(Slot) &&
                   int.TryParse(Rack, out _) &&
                   int.TryParse(Slot, out _);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}