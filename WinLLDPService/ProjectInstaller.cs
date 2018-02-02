namespace WinLLDPService
{
    using System.ComponentModel;
    using System.Configuration.Install;
    using System.ServiceProcess;

    /// <summary>
    /// The project installer.
    /// </summary>
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        /// <summary>
        /// The service process installer.
        /// </summary>
        private ServiceProcessInstaller serviceProcessInstaller;

        /// <summary>
        /// The service installer.
        /// </summary>
        private ServiceInstaller serviceInstaller;

        /// <summary>
        /// Project installer
        /// </summary>
        public ProjectInstaller()
        {
            this.serviceProcessInstaller = new ServiceProcessInstaller();
            this.serviceInstaller = new ServiceInstaller();

            // Here you can set properties on serviceProcessInstaller or register event handlers
            this.serviceProcessInstaller.Account = ServiceAccount.LocalService;

            this.serviceInstaller.ServiceName = WinLLDPService.MyServiceName;
            this.Installers.AddRange(new Installer[] { this.serviceProcessInstaller, this.serviceInstaller });
        }
    }
}
