using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace WinLLDPService
{
	public class WinLLDPService : ServiceBase
	{
		public const string MyServiceName = "WinLLDPService";
		
		Timer timer;
		WinLLDP run;

		public WinLLDPService()
		{
			InitializeComponent();
		}
		
		private void InitializeComponent()
		{
			run = new WinLLDP();
			
			this.ServiceName = MyServiceName;
			this.CanStop = true;

			
			EventLog.Source = this.ServiceName;
			EventLog.Log = "Application";
			
			if (!EventLog.SourceExists(EventLog.Source)) {
				EventLog.CreateEventSource(EventLog.Source, this.EventLog.Log);
			}
			
			timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
			//timer = new Timer(30 * 1000);
			timer.AutoReset = true;
			timer.Elapsed += sendPacket;
		}
		
		private void sendPacket(object source, ElapsedEventArgs ea)
		{
			try {
				run.Run();
			} catch (Exception ex) {
				EventLog.WriteEntry("Packet sent failed: " + ex.ToString(), EventLogEntryType.Error);
			}
		}
		
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// TODO: Add cleanup code here (if required)
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// Start this service.
		/// </summary>
		protected override void OnStart(string[] args)
		{
			timer.Start();
		}
		
		/// <summary>
		/// Stop this service.
		/// </summary>
		protected override void OnStop()
		{
			timer.Stop();
		}
	}
}
