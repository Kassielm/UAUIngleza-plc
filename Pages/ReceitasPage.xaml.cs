using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Pages
{
    public partial class ReceitasPage : ContentPage, INotifyPropertyChanged
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly IPlcService _plcService;

        public ObservableCollection<Recipe> Recipes { get; } = new();

        public ReceitasPage(IRecipeRepository recipeRepository, IPlcService plcService)
        {
            InitializeComponent();
            _recipeRepository = recipeRepository;
            _plcService = plcService;
            BindingContext = this;
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
                var recipes = await _recipeRepository.GetAsync<Recipe>();

                Recipes.Clear();
                foreach (var recipe in recipes)
                {
                    Recipes.Add(recipe);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro ao carregar receitas: {ex.Message}", "OK");
            }
        }

        private async void OnAddRecipeClicked(object sender, EventArgs e)
        {
            try
            {
                string name = await DisplayPromptAsync(
                    "Nova Receita",
                    "Digite o nome da receita:",
                    "OK",
                    "Cancelar",
                    placeholder: "Ex: Receita Especial",
                    maxLength: 30
                );

                if (string.IsNullOrWhiteSpace(name))
                    return;

                string bottlesStr = await DisplayPromptAsync(
                    "Quantidade de frascos",
                    "Digite a quantidade de frascos:",
                    "OK",
                    "Cancelar",
                    placeholder: "Ex: 24",
                    keyboard: Keyboard.Numeric,
                    maxLength: 5
                );

                if (string.IsNullOrWhiteSpace(bottlesStr))
                    return;

                if (!int.TryParse(bottlesStr, out int bottles))
                {
                    await DisplayAlertAsync("Erro", "Quantidade de frascos inválida!", "OK");
                    return;
                }

                var allRecipes = await _recipeRepository.GetAsync<Recipe>();
                var nextId = allRecipes.Count > 0 ? allRecipes.Max(r => r.Id) + 1 : 1;

                var newRecipe = new Recipe
                {
                    Id = nextId,
                    Name = name.Trim(),
                    Bottles = bottles,
                };

                await _recipeRepository.SaveAsync(newRecipe);
                Recipes.Add(newRecipe);

                ShowStatus("✅ Receita adicionada com sucesso!", Colors.Green);
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro ao adicionar receita: {ex.Message}", "OK");
            }
        }

        private async void OnEditRecipeClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is Recipe recipe)
                {
                    string name = await DisplayPromptAsync(
                        "Editar Receita",
                        "Digite o novo nome da receita:",
                        "OK",
                        "Cancelar",
                        placeholder: "Nome da receita",
                        initialValue: recipe.Name,
                        maxLength: 30
                    );

                    if (string.IsNullOrWhiteSpace(name))
                        return;

                    string bottlesStr = await DisplayPromptAsync(
                        "Quantidade de frascos",
                        "Digite a quantidade de frascos:",
                        "OK",
                        "Cancelar",
                        placeholder: "Quantidade",
                        initialValue: recipe.Bottles.ToString(),
                        keyboard: Keyboard.Numeric,
                        maxLength: 5
                    );

                    if (string.IsNullOrWhiteSpace(bottlesStr))
                        return;

                    if (!int.TryParse(bottlesStr, out int bottles))
                    {
                        await DisplayAlertAsync("Erro", "Quantidade de frascos inválida!", "OK");
                        return;
                    }

                    recipe.Name = name.Trim();
                    recipe.Bottles = bottles;

                    await _recipeRepository.UpdateAsync(recipe);

                    var index = Recipes.IndexOf(recipe);
                    if (index >= 0)
                    {
                        Recipes[index] = recipe;
                    }

                    await LoadRecipes();

                    ShowStatus("✅ Receita atualizada com sucesso!", Colors.Green);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro ao editar receita: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteRecipeClicked(object sender, EventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is Recipe recipe)
                {
                    bool confirm = await DisplayAlertAsync(
                        "Confirmar Exclusão",
                        $"Deseja realmente excluir a receita '{recipe.Name}'?",
                        "Sim",
                        "Não"
                    );

                    if (!confirm)
                        return;

                    await _recipeRepository.DeleteAsync(recipe);
                    Recipes.Remove(recipe);

                    ShowStatus("✅ Receita excluída com sucesso!", Colors.Green);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Erro", $"Erro ao deletar receita: {ex.Message}", "OK");
            }
        }

        private void ShowStatus(string message, Color color)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = color;
            StatusBorder.IsVisible = true;

            Task.Run(async () =>
            {
                await Task.Delay(3000);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusBorder.IsVisible = false;
                });
            });
        }

        public new event PropertyChangedEventHandler? PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
