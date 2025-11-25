using Sharp7.Rx;
using Sharp7.Rx.Enums;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly IPLCService _plcService;
        private string _connectionStatus = "🔄 Verificando conexão...";
        private string _recipeValue = "---";
        private string _recipeText = "Nenhuma receita selecionada";
        private bool _isConnected = false;
        private bool _isProcessing = false;

        // Endereço onde as receitas serão escritas
        private const string RecipeAddress = "DB1.DBW0";

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
            BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SubscribeToConnectionStatus();
            SubscribeToBitChanges();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _disposables.Clear();
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
                RecipeText = "Nenhuma receita selecionada";
                return;
            }

            if (int.TryParse(value, out int recipeNum))
            {
                RecipeText = recipeNum switch
                {
                    0 => "📋 Receita 1 Ativa",
                    1 => "📋 Receita 2 Ativa",
                    2 => "📋 Receita 3 Ativa",
                    3 => "📋 Receita 4 Ativa",
                    4 => "📋 Receita 5 Ativa",
                    _ => $"📋 Receita desconhecida ({recipeNum})"
                };
            }
            else
            {
                RecipeText = "Valor inválido";
            }
        }

        private async void OnSetBitClicked(object? sender, EventArgs e)
        {
            if (!CanInteract)
            {
                await DisplayAlert("Aviso", "PLC não está conectado!", "OK");
                return;
            }

            IsProcessing = true;

            try
            {
                Console.WriteLine($"⬆️ Setando bit em {RecipeAddress} para 1...");
                
                await _plcService.Plc!.SetValue<short>(RecipeAddress, 1);
                
                Console.WriteLine("✅ Bit setado com sucesso!");
                await DisplayAlert("Sucesso", "Bit setado para 1", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao setar bit: {ex.Message}");
                await DisplayAlert("Erro", $"Erro ao setar bit: {ex.Message}", "OK");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void OnResetBitClicked(object? sender, EventArgs e)
        {
            if (!CanInteract)
            {
                await DisplayAlert("Aviso", "PLC não está conectado!", "OK");
                return;
            }

            IsProcessing = true;

            try
            {
                Console.WriteLine($"⬇️ Resetando bit em {RecipeAddress} para 0...");
                
                await _plcService.Plc!.SetValue<short>(RecipeAddress, 0);
                
                Console.WriteLine("✅ Bit resetado com sucesso!");
                await DisplayAlert("Sucesso", "Bit resetado para 0", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao resetar bit: {ex.Message}");
                await DisplayAlert("Erro", $"Erro ao resetar bit: {ex.Message}", "OK");
            }
            finally
            {
                IsProcessing = false;
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
                Console.WriteLine($"📋 Escrevendo Receita {recipeNumber + 1} (valor {recipeNumber}) em {RecipeAddress}...");
                
                // Usa SetValue do Sharp7.Rx diretamente
                await _plcService.Plc!.SetValue<short>(RecipeAddress, (short)recipeNumber);
                
                Console.WriteLine($"✅ Receita {recipeNumber + 1} escrita com sucesso! Valor: {recipeNumber}");
                await DisplayAlert("Sucesso", $"Receita {recipeNumber + 1} ativada!", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao escrever receita {recipeNumber + 1}: {ex.Message}");
                await DisplayAlert("Erro", $"Erro: {ex.Message}", "OK");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async void OnRecipe1Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(0); // Receita 1 = valor 0
        }

        private async void OnRecipe2Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(1); // Receita 2 = valor 1
        }

        private async void OnRecipe3Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(2); // Receita 3 = valor 2
        }

        private async void OnRecipe4Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(3); // Receita 4 = valor 3
        }

        private async void OnRecipe5Clicked(object? sender, EventArgs e)
        {
            await WriteRecipeToPLC(4); // Receita 5 = valor 4
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