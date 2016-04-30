using System.ServiceProcess;

namespace WinLLDPService
{
    static class ServiceMain
	{
		/// <summary>
		/// This method starts the service.
		/// </summary>
		static void Main()
		{
			// Run WinLLDPService
			ServiceBase.Run(new ServiceBase[] { new WinLLDPService() });
		}
	}
}
