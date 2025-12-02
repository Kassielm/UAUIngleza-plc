using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages
{
   public partial class ReceitasPage : ContentPage
    {
        private readonly IStorageService _storageService;
        private readonly IPLCService _plcService;
        private RecipesConfiguration _recipesConfig = new();
        private readonly List<(Entry NameEntry, Entry BottleEntry)> _recipeControls;

        public ReceitasPage(IStorageService storageService, IPLCService plcService)
        {
            InitializeComponent();
            _storageService = storageService;
            _plcService = plcService;

            _recipeControls =
            [
                (Recipe1Name, Recipe1Bottles),
                (Recipe2Name, Recipe2Bottles),
                (Recipe3Name, Recipe3Bottles),
                (Recipe4Name, Recipe4Bottles),
                (Recipe5Name, Recipe5Bottles),
                (Recipe6Name, Recipe6Bottles),
                (Recipe7Name, Recipe7Bottles),
                (Recipe8Name, Recipe8Bottles),
                (Recipe9Name, Recipe9Bottles),
                (Recipe10Name, Recipe10Bottles)
            ];
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadRecipes();
        }

        private async Task LoadRecipes()
        {
            try
            {
                _recipesConfig = await _storageService.GetRecipesAsync();

                for (int i = 0; i < _recipeControls.Count; i++)
                {
                    if (i < _recipesConfig.Recipes.Count)
                    {
                        var recipe = _recipesConfig.Recipes[i];
                        var (nameEntry, bottleEntry) = _recipeControls[i];

                        nameEntry.Text = recipe.Name;
                        bottleEntry.Text = recipe.Bottles.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar receitas: {ex.Message}");
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < _recipeControls.Count; i++)
                {
                    if (i < _recipesConfig.Recipes.Count)
                    {
                        var (nameEntry, bottleEntry) = _recipeControls[i];

                        _recipesConfig.Recipes[i].Name = nameEntry.Text?.Trim() ?? $"Receita {i + 1}";
                        _recipesConfig.Recipes[i].Bottles = int.Parse(bottleEntry.Text ?? "0");
                    }
                }

                await _storageService.SaveRecipesAsync(_recipesConfig);

                if (_plcService.IsConnected)
                {
                    await WriteBottleCountsToPLC();
                    await DisplayAlertAsync("Sucesso", "Receitas salvas e enviadas ao PLC!", "OK");
                }
                else
                {
                    ShowStatus("Receitas salvas! (PLC offline)", Colors.Orange);
                    await DisplayAlertAsync("Aviso", "Receitas salvas localmente, mas o PLC está offline.", "OK");
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Erro: {ex.Message}", Colors.Red);
            }
        }

        private async Task WriteBottleCountsToPLC()
        {
            try
            {
                foreach (var recipe in _recipesConfig.Recipes)
                {
                    await _plcService.Plc!.SetValue<short>(recipe.PlcAddress, (short)recipe.Bottles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao escrever no PLC: {ex.Message}");
                throw;
            }
        }

        private void ShowStatus(string message, Color color)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = color;
            StatusLabel.IsVisible = true;

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusLabel.IsVisible = false;
                });
            });
        }
    }
}