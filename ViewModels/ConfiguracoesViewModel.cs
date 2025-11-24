//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using System.Windows.Input;
//using UAUIngleza_plc.Models;
//using UAUIngleza_plc.Services;

//namespace UAUIngleza_plc.ViewModels
//{
//    public class ConfiguracoesViewModel : INotifyPropertyChanged
//    {
//        private readonly IStorageService _storageService;
//        private readonly IPLCService _plcService;

//        private string _ipAddress = "";
//        private int _rack = 0;
//        private int _slot = 0;
//        private string _statusMessage = "Aguardando configuração...";
//        private bool _isConnecting = false;
//        private bool _isConnected = false;

//        public string IpAddress
//        {
//            get => _ipAddress;
//            set
//            {
//                if (_ipAddress != value)
//                {
//                    _ipAddress = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public int Rack
//        {
//            get => _rack;
//            set
//            {
//                if (_rack != value)
//                {
//                    _rack = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public int Slot
//        {
//            get => _slot;
//            set
//            {
//                if (_slot != value)
//                {
//                    _slot = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public string StatusMessage
//        {
//            get => _statusMessage;
//            set
//            {
//                if (_statusMessage != value)
//                {
//                    _statusMessage = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public bool IsConnecting
//        {
//            get => _isConnecting;
//            set
//            {
//                if (_isConnecting != value)
//                {
//                    _isConnecting = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public bool IsConnected
//        {
//            get => _isConnected;
//            set
//            {
//                if (_isConnected != value)
//                {
//                    _isConnected = value;
//                    OnPropertyChanged();
//                }
//            }
//        }

//        public ICommand SaveConfigCommand { get; }
//        public ICommand ConnectCommand { get; }

//        public ConfiguracoesViewModel(IStorageService storageService, IPLCService plcService)
//        {
//            _storageService = storageService;
//            _plcService = plcService;

//            SaveConfigCommand = new Command(async () => await SaveConfiguration());
//            ConnectCommand = new Command(async () => await Connect());

//            LoadConfiguration();
//        }

//        private async void LoadConfiguration()
//        {
//            var config = await _storageService.GetConfigAsync();
//            if (config != null)
//            {
//                IpAddress = config.IpAddress;
//                Rack = config.Rack;
//                Slot = config.Slot;
//                StatusMessage = "Configuração carregada.";
//            }
//        }

//        private async Task SaveConfiguration()
//        {
//            try
//            {
//                if (!ValidateInputs())
//                {
//                    StatusMessage = "Configuração inválida. Verifique os campos.";
//                    return;
//                }

//                var config = new PlcConfiguration
//                {
//                    IpAddress = IpAddress,
//                    Rack = Rack,
//                    Slot = Slot
//                };

//                await _storageService.SaveConfigAsync(config);
//                StatusMessage = "Configuração salva com sucesso.";
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Erro ao salvar configuração: {ex.Message}";
//            }
//        }

//        private async Task Connect()
//        {
//            try
//            {
//                if (!ValidateInputs())
//                {
//                    StatusMessage = "Configuração inválida. Verifique os campos.";
//                    return;
//                }

//                IsConnecting = true;
//                StatusMessage = "Conectando ao PLC...";

//                var config = new PlcConfiguration
//                {
//                    IpAddress = IpAddress,
//                    Rack = Rack,
//                    Slot = Slot
//                };

//                bool connected = await _plcService.ConnectAsync(config);

//                if (connected)
//                {
//                    IsConnected = true;
//                    StatusMessage = "Conectado ao PLC com sucesso.";
//                }
//                else
//                {
//                    IsConnected = false;
//                    StatusMessage = "Falha ao conectar ao PLC.";
//                }
//            }
//            catch (Exception ex)
//            {
//                StatusMessage = $"Erro ao conectar: {ex.Message}";
//            }
//            finally
//            {
//                IsConnecting = false;
//            }
//        }

//        private bool ValidateInputs()
//        {
//            if (string.IsNullOrWhiteSpace(IpAddress))
//                return false;
//            if (Rack < 0 || Slot < 0)
//                return false;
//            return true;
//        }

//        public event PropertyChangedEventHandler? PropertyChanged;

//        protected void OnPropertyChanged([CallerMemberName] string name = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
//        }
//    }
//}
