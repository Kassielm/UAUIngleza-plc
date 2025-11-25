using Microsoft.Maui.Controls;
using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages
{
    public partial class CameraPage : ContentPage, INotifyPropertyChanged
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IStorageService _storageService;
        private readonly IPLCService _plcService;
        private string _cameraIp = "";
        private string _connectionStatus = "Verificando conexão...";
        private bool _isConnected = false;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                    Console.WriteLine($"🔌 Status PLC na Camera: {(value ? "CONECTADO 🟢" : "DESCONECTADO 🔴")}");
                }
            }
        }
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public CameraPage(IStorageService storageService, IPLCService plcService)
        {
            InitializeComponent();
            _storageService = storageService;
            _plcService = plcService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SubscribeToPlcStatus();
            await LoadConfiguration();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _disposables.Clear();
        }

        private void SubscribeToPlcStatus()
        {
            try
            {
                var statusConnection = _plcService.ConnectionStatus
                    .DistinctUntilChanged()
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                            state =>
                            {
                                if (state == ConnectionState.Connected)
                                {
                                    ConnectionStatus = "ONLINE";
                                    IsConnected = true;
                                    Console.WriteLine("✅ PLC conectado!");
                                }
                                else
                                {
                                    ConnectionStatus = "OFFLINE";
                                    IsConnected = false;
                                    Console.WriteLine("❌ PLC desconectado!");
                                }
                            },
                            error =>
                            {
                                Console.WriteLine($"❌ Erro ao monitorar status: {error.Message}");
                                ConnectionStatus = "⚠ ERRO";
                                IsConnected = false;
                            });

                _disposables.Add(statusConnection);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro");
            }
        }

        public void SetUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                CameraWebView.Source = url;
            }
        }

        private async Task LoadConfiguration()
        {
            try
            {
                var config = await _storageService.GetConfigAsync();

                if (config != null)
                {
                    _cameraIp = config.CameraIp ?? string.Empty;
                    Console.WriteLine($"📹 IP da Câmera carregado: {_cameraIp}");

                    SetUrl(_cameraIp);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao carregar config: {ex.Message}");
            }
        }

        private async void OnRecipeClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                string recipeNumber = button.CommandParameter?.ToString() ?? "0";
            
                Console.WriteLine($"📋 Receita {recipeNumber} selecionada");

                if (!IsConnected)
                {
                    await DisplayAlert("Aviso", "PLC não está conectado!", "OK");
                    return;
                }

                try
                {   
                    await DisplayAlert("Receita Selecionada", $"Receita {recipeNumber} foi selecionada", "OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Erro ao selecionar receita: {ex.Message}");
                    await DisplayAlert("Erro", $"Erro ao selecionar receita: {ex.Message}", "OK");
                }
            }
        }
        public new event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
