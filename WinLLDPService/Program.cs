using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace WinLLDPService
{
	static class Program
	{
		/// <summary>
		/// This method starts the service.
		/// </summary>
		static void Main()
		{
			// To run more than one service you have to add them here
			ServiceBase.Run(new ServiceBase[] { new WinLLDPService() });
		}
	}
}
