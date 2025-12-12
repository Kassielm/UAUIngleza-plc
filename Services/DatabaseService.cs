using SQLite;
using UAUIngleza_plc.Models;

namespace UAUIngleza_plc.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;

        public async Task InitAsync()
        {
            if (_database != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "uauingleza.db");
            _database = new SQLiteAsyncConnection(dbPath);

            await _database.CreateTableAsync<Models.SystemConfiguration>();
            await _database.CreateTableAsync<Recipe>();

            await SeedDefaultDataAsync();
        }

        private async Task SeedDefaultDataAsync()
        {
            var configCount = await _database!.Table<Models.SystemConfiguration>().CountAsync();
            if (configCount == 0)
            {
                await _database.InsertAsync(new Models.SystemConfiguration());
            }

            var recipeCount = await _database.Table<Recipe>().CountAsync();
            if (recipeCount == 0)
            {
                var defaultRecipes = new List<Recipe>
                {
                    new Recipe { Id = 1, Name = "Receita 1", Bottles = 0 },
                    new Recipe { Id = 2, Name = "Receita 2", Bottles = 0 },
                    new Recipe { Id = 3, Name = "Receita 3", Bottles = 0 },
                    new Recipe { Id = 4, Name = "Receita 4", Bottles = 0 },
                    new Recipe { Id = 5, Name = "Receita 5", Bottles = 0 },
                    new Recipe { Id = 6, Name = "Receita 6", Bottles = 0 },
                    new Recipe { Id = 7, Name = "Receita 7", Bottles = 0 },
                    new Recipe { Id = 8, Name = "Receita 8", Bottles = 0 },
                    new Recipe { Id = 9, Name = "Receita 9", Bottles = 0 },
                    new Recipe { Id = 10, Name = "Receita 10", Bottles = 0 },
                };
                await _database.InsertAllAsync(defaultRecipes);
            }
        }

        public async Task<Models.SystemConfiguration> GetConfigurationAsync()
        {
            await InitAsync();
            var config = await _database!.Table<Models.SystemConfiguration>().FirstOrDefaultAsync();
            return config ?? new Models.SystemConfiguration();
        }

        public async Task SaveConfigurationAsync(Models.SystemConfiguration config)
        {
            await InitAsync();

            var existingConfig = await _database!
                .Table<Models.SystemConfiguration>()
                .FirstOrDefaultAsync();
            if (existingConfig != null)
            {
                config.Id = existingConfig.Id;
                await _database.UpdateAsync(config);
            }
            else
            {
                await _database.InsertAsync(config);
            }
        }

        public async Task<List<Recipe>> GetRecipesAsync()
        {
            await InitAsync();
            return await _database!.Table<Recipe>().OrderBy(r => r.Id).ToListAsync();
        }

        public async Task SaveRecipeAsync(Recipe recipe)
        {
            await InitAsync();

            var existingRecipe = await _database!.Table<Recipe>()
                .FirstOrDefaultAsync(r => r.Id == recipe.Id);

            if (existingRecipe != null)
            {
                await _database.UpdateAsync(recipe);
            }
            else
            {
                await _database.InsertAsync(recipe);
            }
        }

        public async Task SaveRecipesAsync(List<Recipe> recipes)
        {
            await InitAsync();

            foreach (var recipe in recipes)
            {
                await SaveRecipeAsync(recipe);
            }
        }

        public async Task DeleteRecipeAsync(Recipe recipe)
        {
            await InitAsync();
            await _database!.DeleteAsync(recipe);
        }

        public async Task<int> GetNextRecipeIdAsync()
        {
            await InitAsync();
            var recipes = await _database!.Table<Recipe>()
                .OrderByDescending(r => r.Id)
                .ToListAsync();
            
            if (recipes.Count == 0)
                return 1;
            
            return recipes[0].Id + 1;
        }
    }
}
