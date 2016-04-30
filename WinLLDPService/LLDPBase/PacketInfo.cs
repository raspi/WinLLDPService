using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinLLDPService
{
    class PacketInfo
    {
        public string OperatingSystem { get; set; }
        public string OperatingSystemVersion { get; set; }
        public string Domain { get; set; }
        public string Username { get; set; }
        public string Uptime { get; set; }
    }
}
