using System;
using System.Collections.Generic;
using System.Text;

namespace UAUIngleza_plc.Models
{
    public class SystemConfiguration
    {
        public string IpAddress { get; set; } = "192.168.2.1";
        public int Rack { get; set; } = 0;
        public int Slot { get; set; } = 1;
        public string CameraIp { get; set; } = "192.168.0.101";
    }
}
