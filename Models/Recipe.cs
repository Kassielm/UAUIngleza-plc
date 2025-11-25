namespace UAUIngleza_plc.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int BottleCount { get; set; }
        public string PlcAddress { get; set; } = string.Empty;
    }

    public class RecipesConfiguration
    {
        public List<Recipe> Recipes { get; set; } = new List<Recipe>
        {
            new Recipe { Id = 1, Name = "Receita 1", BottleCount = 0, PlcAddress = "Db1.Int2" },
            new Recipe { Id = 2, Name = "Receita 2", BottleCount = 0, PlcAddress = "Db1.Int4" },
            new Recipe { Id = 3, Name = "Receita 3", BottleCount = 0, PlcAddress = "Db1.Int6" },
            new Recipe { Id = 4, Name = "Receita 4", BottleCount = 0, PlcAddress = "Db1.Int8" },
            new Recipe { Id = 5, Name = "Receita 5", BottleCount = 0, PlcAddress = "Db1.Int10" }
        };
    }
}
