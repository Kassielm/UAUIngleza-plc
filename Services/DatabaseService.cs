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
                List<Recipe> defaultRecipes = [];

                for (int i = 1; i <= 10; i++)
                {
                    var recipe = new Recipe
                    {
                        Id = i,
                        Name = $"Receita {i}",
                        Bottles = 0,
                    };

                    defaultRecipes.Add(recipe);
                }
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

            var existingRecipe = await _database!
                .Table<Recipe>()
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

        public async Task DeleteRecipeAsync(Recipe recipe)
        {
            await InitAsync();
            await _database!.DeleteAsync(recipe);
        }
    }
}
