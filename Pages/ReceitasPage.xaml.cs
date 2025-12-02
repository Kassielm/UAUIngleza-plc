using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages
{
   public partial class ReceitasPage : ContentPage
    {
        private readonly IStorageService _storageService;
        private readonly IPLCService _plcService;
        private RecipesConfiguration _recipesConfig = new RecipesConfiguration();

        private List<Entry> _recipeControls;

        public ReceitasPage(IStorageService storageService, IPLCService plcService)
        {
            InitializeComponent();
            _storageService = storageService;
            _plcService = plcService;

            _recipeControls = new List<Entry>
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

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < _recipeControls.Count; i++)
                {
                    if (i < _recipesConfig.Recipes.Count)
                    {
                        var nameEntry = _recipeControls[i];

                        _recipesConfig.Recipes[i].Name = nameEntry.Text?.Trim() ?? $"Receita {i + 1}";
                    }
                }

                await _storageService.SaveRecipesAsync(_recipesConfig);

                if (_plcService.IsConnected)
                {
                    ShowStatus("Receitas salvas e sincronizadas!", Colors.Green);
                }
                else
                {
                    ShowStatus("Receitas salvas! (PLC offline)", Colors.Orange);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"Erro: {ex.Message}", Colors.Red);
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