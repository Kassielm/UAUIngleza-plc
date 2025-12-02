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
        private readonly IStorageService _storageService;
        private readonly IPLCService _plcService;
        private RecipesConfiguration _recipesConfig = new RecipesConfiguration();
        private List<Button> _recipeControls;
        private string _connectionStatus = "🔄 Verificando conexão...";
        private string _recipeValue = "---";
        private bool _isConnected = false;
        private const string RecipeAddress = "DB1.DBW0";

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

        public MainPage(IStorageService storageService, IPLCService plcService)
        {
            InitializeComponent();
            _storageService = storageService;
            _plcService = plcService;

            _recipeControls = new List<Button>
            {
                Recipe1Name,
                Recipe2Name,
                Recipe3Name,
                Recipe4Name,
                Recipe5Name,
                Recipe6Name,
                Recipe7Name,
                Recipe8Name,
                Recipe9Name,
                Recipe10Name
            };
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

                for (int i = 0; i < _recipeControls.Count; i++)
                {
                    if (i < _recipesConfig.Recipes.Count)
                    {
                        var recipe = _recipesConfig.Recipes[i];
                        var nameEntry = _recipeControls[i];

                        nameEntry.Text = recipe.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar receitas: {ex.Message}");
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