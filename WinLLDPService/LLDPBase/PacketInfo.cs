using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WinLLDPService
{
    /// <summary>
    /// 
    /// </summary>
    class PacketInfo
    {
        /// <summary>
        /// "Friendly" Operating system name
        /// For example: Microsoft Windows 7 Ultimate Service Pack 1
        /// </summary>
        public string OperatingSystem { get; set; }

        /// <summary>
        /// OS Version
        /// For example: Microsoft Windows NT 6.1.7601 Service Pack 1
        /// </summary>
        public string OperatingSystemVersion { get; set; }

        /// <summary>
        /// Username and domain if machine is joined in domain
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Uptime in seconds
        /// </summary>
        public string Uptime { get; set; }
    }
}