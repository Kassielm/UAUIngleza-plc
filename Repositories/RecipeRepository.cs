using UAUIngleza_plc.Interfaces;
using UAUIngleza_plc.Models;
using UAUIngleza_plc.Services;

namespace UAUIngleza_plc.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly DatabaseService _databaseService;

        public RecipeRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<List<T>> GetAsync<T>() where T : class
        {
            if (typeof(T) == typeof(Recipe))
            {
                await _databaseService.InitAsync();
                var recipes = await _databaseService.GetRecipesAsync();
                return recipes as List<T> ?? new List<T>();
            }
            throw new NotSupportedException($"Tipo {typeof(T).Name} não suportado por RecipeRepository.");
        }

        public async Task<T?> GetOneAsync<T>(int id) where T : class
        {
            if (typeof(T) == typeof(Recipe))
            {
                await _databaseService.InitAsync();
                var recipes = await _databaseService.GetRecipesAsync();
                var recipe = recipes.FirstOrDefault(r => r.Id == id);
                return recipe as T;
            }
            throw new NotSupportedException($"Tipo {typeof(T).Name} não suportado por RecipeRepository.");
        }

        public async Task SaveAsync<T>(T type) where T : class
        {
            if (type is Recipe recipe)
            {
                await _databaseService.InitAsync();
                await _databaseService.SaveRecipeAsync(recipe);
                return;
            }
            throw new NotSupportedException($"Tipo {typeof(T).Name} não suportado por RecipeRepository.");
        }

        public async Task UpdateAsync<T>(T type) where T : class
        {
            if (type is Recipe recipe)
            {
                await _databaseService.InitAsync();
                await _databaseService.SaveRecipeAsync(recipe);
                return;
            }
            throw new NotSupportedException($"Tipo {typeof(T).Name} não suportado por RecipeRepository.");
        }

        public async Task DeleteAsync<T>(T type) where T : class
        {
            if (type is Recipe recipe)
            {
                await _databaseService.InitAsync();
                await _databaseService.DeleteRecipeAsync(recipe);
                return;
            }
            throw new NotSupportedException($"Tipo {typeof(T).Name} não suportado por RecipeRepository.");
        }
    }
}
