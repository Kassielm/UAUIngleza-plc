using SQLite;

namespace UAUIngleza_plc.Models
{
    [Table("system_configuration")]
    public class SystemConfiguration
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string IpAddress { get; set; } = "192.168.2.1";
        public int Rack { get; set; } = 0;
        public int Slot { get; set; } = 1;
        public string CameraIp { get; set; } = "192.168.2.101";
    }
}
