namespace UAUIngleza_plc.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Bottles { get; set; }
        public string PlcAddress { get; set; } = string.Empty;
    }

    public class RecipesConfiguration
    {
        public List<Recipe> Recipes { get; set; } =
        [
            new Recipe { Id = 1, Name = "Receita 1", Bottles = 0, PlcAddress = "Db1.Int2" },
            new Recipe { Id = 2, Name = "Receita 2", Bottles = 0, PlcAddress = "Db1.Int4" },
            new Recipe { Id = 3, Name = "Receita 3", Bottles = 0, PlcAddress = "Db1.Int6" },
            new Recipe { Id = 4, Name = "Receita 4", Bottles = 0, PlcAddress = "Db1.Int8" },
            new Recipe { Id = 5, Name = "Receita 5", Bottles = 0, PlcAddress = "Db1.Int10" },
            new Recipe { Id = 6, Name = "Receita 6", Bottles = 0, PlcAddress = "Db1.Int12" },
            new Recipe { Id = 7, Name = "Receita 7", Bottles = 0, PlcAddress = "Db1.Int14" },
            new Recipe { Id = 8, Name = "Receita 8", Bottles = 0, PlcAddress = "Db1.Int16" },
            new Recipe { Id = 9, Name = "Receita 9", Bottles = 0, PlcAddress = "Db1.Int18" },
            new Recipe { Id = 10, Name = "Receita 10", Bottles = 0, PlcAddress = "Db1.Int20" },
        ];
    }
}
