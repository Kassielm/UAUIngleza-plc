using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;
using UAUIngleza_plc.Models;
using System.Windows.Input;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        public ICommand ChangeRecipe { get; private set; }
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IPLCService _plcService;
        private readonly IStorageService _storageService;
        private RecipesConfiguration _recipesConfig;
        private string _connectionStatus = "🔄 Verificando conexão...";
        private string _recipeValue = "---";
        private bool _isConnected = false;
        private string _recipe1Name = "Receita 1";
        private string _recipe2Name = "Receita 2";
        private string _recipe3Name = "Receita 3";
        private string _recipe4Name = "Receita 4";
        private string _recipe5Name = "Receita 5";
        private string _recipe6Name = "Receita 6";
        private string _recipe7Name = "Receita 7";
        private string _recipe8Name = "Receita 8";
        private string _recipe9Name = "Receita 9";
        private string _recipe10Name = "Receita 10";

        private const string RecipeAddress = "DB1.DBW0";

        public string Recipe1Name
        {
            get => _recipe1Name;
            set
            {
                if (_recipe1Name != value)
                {
                    _recipe1Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recipe2Name
        {
            get => _recipe2Name;
            set
            {
                if (_recipe2Name != value)
                {
                    _recipe2Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recipe3Name
        {
            get => _recipe3Name;
            set
            {
                if (_recipe3Name != value)
                {
                    _recipe3Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recipe4Name
        {
            get => _recipe4Name;
            set
            {
                if (_recipe4Name != value)
                {
                    _recipe4Name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Recipe5Name
        {
            get => _recipe5Name;
            set
            {
                if (_recipe5Name != value)
                {
                    _recipe5Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Recipe6Name
        {
            get => _recipe6Name;
            set
            {
                if (_recipe6Name != value)
                {
                    _recipe6Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Recipe7Name
        {
            get => _recipe7Name;
            set
            {
                if (_recipe7Name != value)
                {
                    _recipe7Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Recipe8Name
        {
            get => _recipe8Name;
            set
            {
                if (_recipe8Name != value)
                {
                    _recipe8Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Recipe9Name
        {
            get => _recipe9Name;
            set
            {
                if (_recipe9Name != value)
                {
                    _recipe9Name = value;
                    OnPropertyChanged();
                }
            }
        }
        public string Recipe10Name
        {
            get => _recipe10Name;
            set
            {
                if (_recipe10Name != value)
                {
                    _recipe10Name = value;
                    OnPropertyChanged();
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

        public string RecipeValue
        {
            get => _recipeValue;
            set
            {
                if (_recipeValue != value)
                {
                    _recipeValue = value;
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

        public MainPage(IPLCService plcService, IStorageService storageService)
        {
            InitializeComponent();
            _plcService = plcService;
            _storageService = storageService;
            ChangeRecipe = new Command<string>(async (param) => await WriteRecipeToPLC(param));
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
                
                Recipe1Name = _recipesConfig?.Recipes[0]?.Name ?? "Receita 1";
                Recipe2Name = _recipesConfig?.Recipes[1]?.Name ?? "Receita 2";
                Recipe3Name = _recipesConfig?.Recipes[2]?.Name ?? "Receita 3";
                Recipe4Name = _recipesConfig?.Recipes[3]?.Name ?? "Receita 4";
                Recipe5Name = _recipesConfig?.Recipes[4]?.Name ?? "Receita 5";
                Recipe6Name = _recipesConfig?.Recipes[5]?.Name ?? "Receita 6";
                Recipe7Name = _recipesConfig?.Recipes[6]?.Name ?? "Receita 7";
                Recipe8Name = _recipesConfig?.Recipes[7]?.Name ?? "Receita 8";
                Recipe9Name = _recipesConfig?.Recipes[8]?.Name ?? "Receita 9";
                Recipe10Name = _recipesConfig?.Recipes[9]?.Name ?? "Receita 10";
            }
            catch (Exception ex)
            {
                _recipesConfig = new RecipesConfiguration();
                Console.WriteLine($"Erro ao carregar configuração de receitas: {ex.Message}");
            }
        }

        private async Task LoadCameraConfiguration()
        {
            try
            {
                var config = await _storageService.GetConfigAsync();
                string urlStream = $"http://{config.CameraIp}:60000/api/v1/script_stream";

                string htmlContent = $@"
                <html>
                <head>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <style>
                        body {{
                            margin: 0;
                            padding: 0;
                            background-color: black;
                            height: 100vh;
                        }}
                        img {{
                            width: 100%;
                            height: 100vh;
                            object-fit: contain;
                            object-position: left;
                            object-position: top;
                        }}
                    </style>
                </head>
                <body>
                    <img src='{urlStream}' />
                </body>
                </html>";

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CameraWebView.Source = new HtmlWebViewSource
                    {
                        Html = htmlContent
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no teste: {ex.Message}");
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
                                ConnectionStatus = "ONLINE";
                                IsConnected = true;
                            }
                            else
                            {
                                ConnectionStatus = "OFFLINE";
                                IsConnected = false;
                                RecipeValue = "---";
                            }
                        },
                        error =>
                        {
                            ConnectionStatus = "ERRO NO STATUS";
                            IsConnected = false;
                        });

                _disposables.Add(connectionSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao subscrever status de conexão: {ex.Message}");
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
                                Console.WriteLine($"⚠Erro ao ler {RecipeAddress}: {ex.Message}");
                                return Observable.Return<short>(0);
                            });
                    })
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        value =>
                        {
                            RecipeValue = value.ToString();
                        },
                        error =>
                        {
                            Console.WriteLine($"Erro na notificação: {error.Message}");
                            RecipeValue = "ERRO";
                        });

                _disposables.Add(bitSubscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao subscrever mudanças do bit: {ex.Message}");
            }
        }

        public async Task WriteRecipeToPLC(string recipeValue)
        {
            if (short.TryParse(recipeValue, out short recipeNumber))
            try
            {
                if (_plcService.Plc != null)
                {
                    await _plcService.Plc!.SetValue<short>(RecipeAddress, (short)recipeNumber);
                    return;
                }

            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro: {ex.Message}", "OK");
            }
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