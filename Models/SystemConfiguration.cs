using System;
using System.Collections.Generic;
using System.Text;

namespace UAUIngleza_plc.Models
{
    public class SystemConfiguration
    {
        public string IpAddress { get; set; }
        public int Rack { get; set; }
        public int Slot { get; set; }
        public string CameraIp { get; set; }
    }
}
