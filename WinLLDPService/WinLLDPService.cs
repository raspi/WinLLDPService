using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace WinLLDPService
{
    public sealed partial class WinLLDPService : ServiceBase
    {

        public const string MyServiceName = "WinLLDPService";

        Timer timer;
        WinLLDP run;

        /// <summary>
        /// Constructor
        /// </summary>
        public WinLLDPService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize
        /// </summary>
        private void InitializeComponent()
        {

            run = new WinLLDP(OsInfo.GetStaticInfo());

            ServiceName = MyServiceName;
            CanStop = true;

            EventLog.Source = ServiceName;
            EventLog.Log = "Application";

            if (!EventLog.SourceExists(EventLog.Source))
            {
                // Create Windows Event Log source if it doesn't exist
                EventLog.CreateEventSource(EventLog.Source, this.EventLog.Log);
            }

            ReduceMemory();

            // Run the LLDP packet sender every 30 seconds
            timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds)
            {
                AutoReset = true
            };

            timer.Elapsed += SendPacket;
        }


        /// <summary>
        /// Main method which is ran every X seconds which is controlled by timer set up in InitializeComponent()  
        /// </summary>
        private void SendPacket(object source, ElapsedEventArgs ea)
        {
            try
            {
                // Run the LLDP packet sender  
                run.Run();
                ReduceMemory();
            }
            catch (Exception ex)
            {
                // Log run error(s) to Windows Event Log
                EventLog.WriteEntry("Packet sent failed: " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        /// <summary>
        /// Try to reduce memory footprint
        /// </summary>
        private void ReduceMemory()
        {
            Process pProcess = Process.GetCurrentProcess();

            try
            {
                NativeMethods.EmptyWorkingSet(pProcess.Handle);
            } catch (Exception ex)
            {
                // Log error(s) to Windows Event Log
                EventLog.WriteEntry("Packet sent failed: " + ex.ToString(), EventLogEntryType.Error);
            }
        }
        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
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
            run.Stop();
        }
    }
}
