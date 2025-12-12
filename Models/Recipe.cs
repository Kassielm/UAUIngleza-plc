using SQLite;

namespace UAUIngleza_plc.Models
{
    [Table("recipes")]
    public class Recipe
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Bottles { get; set; }
    }
}
