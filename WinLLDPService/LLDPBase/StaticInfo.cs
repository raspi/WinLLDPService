namespace WinLLDPService
{
    /// <summary>
    /// Static information that doesn't change after starting OS
    /// </summary>
    public class StaticInfo
    {
        /// <summary>
        /// Gets or sets the machine name.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// Gets or sets the OS "friendly" name
        /// </summary>
        public string OperatingSystemFriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the OS version
        /// </summary>
        public string OperatingSystemVersion { get; set; }

        /// <summary>
        /// Gets or sets the service tag
        /// </summary>
        public string ServiceTag { get; set; }
    }
}