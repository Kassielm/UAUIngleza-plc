using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Sharp7.Rx.Enums;
using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Models;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        public Command ChangeRecipe { get; private set; }
        private readonly CompositeDisposable _disposables = [];
        private readonly IConfigRepository _configRepository;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IPlcService _plcService;
        private bool _isUpdatingFromPLC = false;

        public ObservableCollection<Recipe> RecipeList { get; } = [];

        private Recipe? _selectedRecipe;
        public Recipe? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (SetField(ref _selectedRecipe, value) && value != null && !_isUpdatingFromPLC)
                {
                    _ = WriteRecipeDataToPLC(value);
                }
            }
        }

        private static readonly string defaultRecipeValue = "---";

        private readonly int _recipeIndex = 0;
        public int RecipeValue
        {
            get => _recipeIndex;
            set => RecipePicker.SelectedIndex = value;
        }

        private string _totalCaixas = defaultRecipeValue;
        public string TotalCaixas
        {
            get => _totalCaixas;
            set => SetField(ref _totalCaixas, value);
        }

        private string _caixasBoas = defaultRecipeValue;
        public string CaixasBoas
        {
            get => _caixasBoas;
            set => SetField(ref _caixasBoas, value);
        }

        private string _caixasRejeitadas = defaultRecipeValue;
        public string CaixasRejeitadas
        {
            get => _caixasRejeitadas;
            set => SetField(ref _caixasRejeitadas, value);
        }

        private string _connectionStatus = "Verificando conexão...";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetField(ref _connectionStatus, value);
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetField(ref _isConnected, value);
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public MainPage(
            IConfigRepository configRepository,
            IRecipeRepository recipeRepository,
            IPlcService plcService
        )
        {
            InitializeComponent();
            _configRepository = configRepository;
            _recipeRepository = recipeRepository;
            _plcService = plcService;

            ChangeRecipe = new(
                async (param) => await WriteRecipeToPLC(param?.ToString() ?? string.Empty)
            );
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
                var recipes = await _recipeRepository.GetAsync<Recipe>();

                RecipeList.Clear();
                foreach (var recipe in recipes)
                {
                    RecipeList.Add(recipe);
                }

                if (recipes.Count > 0)
                {
                    SelectedRecipe = recipes[0];
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro ao carregar receitas: {ex.Message}", "OK");
            }
        }

        private async Task LoadCameraConfiguration()
        {
            try
            {
                var config = await _configRepository.GetOneAsync<Models.SystemConfiguration>(0);
                string urlStream = $"http://{config.CameraIp}:60000/api/v1/script_stream";

                string htmlContent =
                    $@"
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
                    CameraWebView.Source = new HtmlWebViewSource { Html = htmlContent };
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Erro",
                    $"Erro ao carregar configuração da câmera: {ex.Message}",
                    "OK"
                );
            }
        }

        private async void SubscribeToConnectionStatus()
        {
            try
            {
                var connectionSubscription = _plcService
                    .ConnectionStatus.DistinctUntilChanged()
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
                            }
                        },
                        error =>
                        {
                            ConnectionStatus = "ERROR";
                            IsConnected = false;
                        }
                    );

                _disposables.Add(connectionSubscription);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Erro",
                    $"Erro ao subscrever status de conexão: {ex.Message}",
                    "OK"
                );
            }
        }

        private async void SubscribeToBitChanges()
        {
            try
            {
                SubscribeToAddress<short>(
                    "DB100.INT14",
                    value =>
                    {
                        _isUpdatingFromPLC = true;
                        try
                        {
                            if (value >= 0 && value < RecipeList.Count)
                            {
                                SelectedRecipe = RecipeList[value - 1];
                            }
                        }
                        finally
                        {
                            _isUpdatingFromPLC = false;
                        }
                    }
                );
                SubscribeToAddress<short>("DB1.INT0", value => TotalCaixas = value.ToString());
                SubscribeToAddress<short>("DB1.INT2", value => CaixasBoas = value.ToString());
                SubscribeToAddress<short>("DB1.INT4", value => CaixasRejeitadas = value.ToString());
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Erro",
                    $"Erro ao inscrever eventos do plc: {ex.Message}",
                    "OK"
                );
            }
        }

        private async void SubscribeToAddress<T>(string address, Action<T> onValueChanged)
            where T : struct
        {
            try
            {
                var subscription = _plcService
                    .ObserveAddress<T>(address)
                    .ObserveOn(SynchronizationContext.Current!)
                    .Subscribe(
                        value => onValueChanged(value),
                        async error =>
                        {
                            await DisplayAlertAsync(
                                "Erro",
                                $"Erro na notificação para {address}: {error.Message}",
                                "OK"
                            );
                            if (typeof(T) == typeof(short) || typeof(T) == typeof(int))
                            {
                                onValueChanged((T)(object)0);
                            }
                        }
                    );

                _disposables.Add(subscription);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync(
                    "Erro",
                    $"Erro ao subscrever {address}: {ex.Message}",
                    "OK"
                );
            }
        }

        private async void ResetCount(object sender, EventArgs args)
        {
            bool confirm = await DisplayAlertAsync(
                "Confirmar Reset",
                "Deseja realmente resetar todos os contadores?",
                "Sim",
                "Não"
            );

            if (!confirm)
                return;

            await Task.Run(async () =>
            {
                try
                {
                    if (_plcService.Plc != null)
                    {
                        await _plcService.Plc!.SetValue<short>("DB1.INT0", 0);
                        await _plcService.Plc!.SetValue<short>("DB1.INT2", 0);
                        await _plcService.Plc!.SetValue<short>("DB1.INT4", 0);

                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await DisplayAlertAsync(
                                "Sucesso",
                                "Contadores resetados com sucesso!",
                                "OK"
                            );
                        });
                    }
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlertAsync(
                            "Erro",
                            $"Erro ao resetar contadores: {ex.Message}",
                            "OK"
                        );
                    });
                }
            });
        }

        public async Task WriteRecipeToPLC(string recipeValue)
        {
            if (short.TryParse(recipeValue, out short recipeNumber))
                try
                {
                    if (_plcService.Plc != null)
                    {
                        await _plcService.Plc!.SetValue<short>("DB100.INT14", (short)recipeNumber);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    await DisplayAlertAsync("Erro", $"Erro: {ex.Message}", "OK");
                }
        }

        public async Task WriteRecipeDataToPLC(Recipe recipe)
        {
            try
            {
                if (_plcService.Plc != null)
                {
                    await _plcService.Plc.SetValue<short>("DB100.INT14", (short)recipe.Id);
                    await _plcService.Plc.SetValue<short>("DB100.INT18", (short)recipe.Bottles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever receita no PLC: {ex.Message}");
            }
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
