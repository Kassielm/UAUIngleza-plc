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
        private readonly CompositeDisposable _disposables = [];
        private readonly IStorageService _storageService;
        private readonly IPLCService _plcService;
        private RecipesConfiguration _recipesConfig = new();
        private readonly List<Button> _recipeControls;
        private readonly Dictionary<string, object> _propertyValues = [];

        public string RecipeValue
        {
            get => GetProperty("---");
            set => SetProperty(value);
        }

        public string TotalCaixas
        {
            get => GetProperty("---");
            set => SetProperty(value);
        }

        public string CaixasBoas
        {
            get => GetProperty("---");
            set => SetProperty(value);
        }

        public string CaixasRejeitadas
        {
            get => GetProperty("---");
            set => SetProperty(value);
        }

        public string ConnectionStatus
        {
            get => GetProperty("🔄 Verificando conexão...");
            set => SetProperty(value);
        }

        public bool IsConnected
        {
            get => GetProperty(false);
            set => SetProperty(value);
        }

        private T GetProperty<T>(T defaultValue = default!, [CallerMemberName] string propertyName = "")
        {
            try
            {
                if (_propertyValues.TryGetValue(propertyName, out var value))
                    return (T)value;
                return defaultValue;
            } catch (Exception ex)
            {
                Console.WriteLine($"deu ruim: {ex.Message}");
                return defaultValue;
            }
        }

        private void SetProperty<T>(T value, [CallerMemberName] string propertyName = "")
        {
            if (_propertyValues.TryGetValue(propertyName, out var existingValue))
            {
                if (EqualityComparer<T>.Default.Equals((T)existingValue, value))
                    return;
            }

            _propertyValues[propertyName] = value!;
            OnPropertyChanged(propertyName);
        }

        public MainPage(IStorageService storageService, IPLCService plcService)
        {
            InitializeComponent();
            _storageService = storageService;
            _plcService = plcService;

            _recipeControls =
            [
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
            ];
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
                SubscribeToAddress<short>("DB1.INT0", value => RecipeValue = value.ToString());
                SubscribeToAddress<short>("DB2.INT0", value => TotalCaixas = value.ToString());
                SubscribeToAddress<short>("DB2.INT2", value => CaixasBoas = value.ToString());
                SubscribeToAddress<short>("DB2.INT4", value => CaixasRejeitadas = value.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao subscrever mudanças do bit: {ex.Message}");
            }
        }

        private void SubscribeToAddress<T>(string address, Action<T> onValueChanged) where T : struct
        {
            try
            {
                var subscription = _plcService.ObserveAddress<T>(address)
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        value => onValueChanged(value),
                        error =>
                        {
                            Console.WriteLine($"Erro na notificação para {address}: {error.Message}");
                            if (typeof(T) == typeof(short) || typeof(T) == typeof(int))
                            {
                                onValueChanged((T)(object)0);
                            }
                        });

                _disposables.Add(subscription);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao subscrever {address}: {ex.Message}");
            }
        }

        public async Task ResetCount()
        {
            try
            {
                if (_plcService.Plc != null)
                {
                    await _plcService.Plc!.SetValue<short>("DB2.INT0", 0);
                    await _plcService.Plc!.SetValue<short>("DB2.INT2", 0);
                    await _plcService.Plc!.SetValue<short>("DB2.INT4", 0);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro: {ex.Message}", "OK");
            }
        }

        public async Task WriteRecipeToPLC(string recipeValue)
        {
            if (short.TryParse(recipeValue, out short recipeNumber))
            try
            {
                if (_plcService.Plc != null)
                {
                    await _plcService.Plc!.SetValue<short>("DB1.INT0", (short)recipeNumber);
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