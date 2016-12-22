namespace WinLLDPService
{
    /// <summary>
    /// Static information that doesn't change after starting OS
    /// </summary>
    public class StaticInfo
    {
        public string MachineName { get; set; }
        public string OperatingSystemFriendlyName { get; set; }
        public string OperatingSystemVersion { get; set; }
        public string ServiceTag { get; set; }
    }
}