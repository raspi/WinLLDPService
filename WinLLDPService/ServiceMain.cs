namespace WinLLDPService
{
    using System.ServiceProcess;

    /// <summary>
    /// The service main.
    /// </summary>
    static class ServiceMain
    {
        /// <summary>
        /// This method starts the service.
        /// </summary>
        public static void Main()
        {
            // Run WinLLDPService
            ServiceBase.Run(new ServiceBase[] { new WinLLDPService() });
        }
    }
}
