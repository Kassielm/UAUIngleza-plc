using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;
using UAUIngleza_plc.Models;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IPLCService _plcService;
        private readonly IStorageService _storageService;
        private RecipesConfiguration _recipesConfig;
        private string _connectionStatus = "🔄 Verificando conexão...";
        private string _recipeValue = "---";
        private string _recipeText = "Nenhuma receita";
        private bool _isConnected = false;
        private bool _isProcessing = false;

        private const string RecipeAddress = "DB1.Int0";

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

        public string RecipeValue
        {
            get => _recipeValue;
            set
            {
                if (_recipeValue != value)
                {
                    _recipeValue = value;
                    OnPropertyChanged();
                    UpdateRecipeText(value);
                }
            }
        }

        public string RecipeText
        {
            get => _recipeText;
            set
            {
                if (_recipeText != value)
                {
                    _recipeText = value;
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
                    OnPropertyChanged(nameof(CanInteract));
                }
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanInteract));
                }
            }
        }

        public bool CanInteract => IsConnected && !IsProcessing;

        public MainPage(IPLCService plcService, IStorageService storageService)
        {
            InitializeComponent();
            _plcService = plcService;
            _storageService = storageService;
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRecipesConfiguration();
            await LoadCameraConfiguration();
            SubscribeToConnectionStatus();
            SubscribeToBitChanges();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _disposables.Clear();
        }

        private async Task LoadRecipesConfiguration()
        {
            try
            {
                _recipesConfig = await _storageService.GetRecipesAsync();
                Console.WriteLine("✅ Receitas carregadas do localStorage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao carregar receitas: {ex.Message}");
                _recipesConfig = new RecipesConfiguration();
            }
        }

        private async Task LoadCameraConfiguration()
        {
            try
            {
                var config = await _storageService.GetConfigAsync();

                if (config != null && !string.IsNullOrEmpty(config.CameraIp))
                {
                    Console.WriteLine($"📹 Carregando câmera: {config.CameraIp}");
                    CameraWebView.Source = config.CameraIp;
                }
                else
                {
                    Console.WriteLine("⚠️ Nenhum IP de câmera configurado");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao carregar configuração da câmera: {ex.Message}");
            }
        }

        private void SubscribeToConnectionStatus()
        {
            try
            {
                var connectionSubscription = _plcService.ConnectionStatus
                    .DistinctUntilChanged()
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        state =>
                        {
                            if (state == ConnectionState.Connected)
                            {
                                ConnectionStatus = "🟢 PLC ONLINE";
                                IsConnected = true;
                                Console.WriteLine("✅ MainPage: PLC conectado!");
                            }
                            else
                            {
                                ConnectionStatus = "🔴 PLC OFFLINE";
                                IsConnected = false;
                                RecipeValue = "---";
                                Console.WriteLine("❌ MainPage: PLC desconectado!");
                            }
                        },
                        error =>
                        {
                            Console.WriteLine($"❌ Erro ao monitorar status: {error.Message}");
                            ConnectionStatus = "⚠️ ERRO NO STATUS";
                            IsConnected = false;
                        });

                _disposables.Add(connectionSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao subscrever status de conexão: {ex.Message}");
            }
        }

        private void SubscribeToBitChanges()
        {
            try
            {
                var bitSubscription = _plcService.ConnectionStatus
                    .Where(state => state == ConnectionState.Connected)
                    .SelectMany(_ =>
                    {
                        if (_plcService.Plc == null)
                            return Observable.Empty<short>();

                        return _plcService.Plc.CreateNotification<short>(
                            RecipeAddress, 
                            TransmissionMode.OnChange)
                            .Catch<short, Exception>(ex =>
                            {
                                Console.WriteLine($"⚠️ Erro ao ler {RecipeAddress}: {ex.Message}");
                                return Observable.Return<short>(0);
                            });
                    })
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        value =>
                        {
                            RecipeValue = value.ToString();
                            Console.WriteLine($"📊 Valor alterado em {RecipeAddress}: {value}");
                        },
                        error =>
                        {
                            Console.WriteLine($"❌ Erro na notificação: {error.Message}");
                            RecipeValue = "ERRO";
                        });

                _disposables.Add(bitSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao subscrever mudanças do bit: {ex.Message}");
            }
        }

        private void UpdateRecipeText(string value)
        {
            if (value == "---" || value == "ERRO")
            {
                RecipeText = "Nenhuma receita";
                return;
            }

            if (int.TryParse(value, out int recipeNum) && recipeNum >= 0 && recipeNum <= 4)
            {
                RecipeText = _recipesConfig?.Recipes[recipeNum]?.Name ?? $"Receita {recipeNum + 1}";
            }
            else
            {
                RecipeText = "Inválido";
            }
        }

        private async Task WriteRecipeToPLC(int recipeNumber)
        {
            if (!CanInteract)
            {
                await DisplayAlert("Aviso", "PLC não está conectado!", "OK");
                return;
            }

            IsProcessing = true;

            try
            {
                string recipeName = _recipesConfig?.Recipes[recipeNumber]?.Name ?? $"Receita {recipeNumber + 1}";
                Console.WriteLine($"📋 Escrevendo {recipeName} (valor {recipeNumber}) em {RecipeAddress}...");
                
                await _plcService.Plc!.SetValue<short>(RecipeAddress, (short)recipeNumber);
                
                Console.WriteLine($"✅ {recipeName} escrita com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao escrever receita: {ex.Message}");
                await DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void OnRecipe1Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(0);
        }

        private async void OnRecipe2Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(1);
        }

        private async void OnRecipe3Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(2);
        }

        private async void OnRecipe4Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(3);
        }

        private async void OnRecipe5Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(4);
        }

        private void OnMenuClicked(object? sender, EventArgs e)
        {
            Shell.Current.FlyoutIsPresented = !Shell.Current.FlyoutIsPresented;
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}