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
        /// Username(s) and domain(s) if machine is joined in domain
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Uptime in XdXhXmXs
        /// </summary>
        public string Uptime { get; set; }

        public string Tag { get; set; }

        public string MachineName { get; set; }

    }
}