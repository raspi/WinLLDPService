namespace WinLLDPService
{
    using System;
    using System.Diagnostics;
    using System.ServiceProcess;
    using System.Timers;

    /// <summary>
    /// The win lldp service.
    /// </summary>
    public sealed partial class WinLLDPService : ServiceBase
    {
        /// <summary>
        /// The my service name.
        /// </summary>
        public const string MyServiceName = "WinLLDPService";

        /// <summary>
        /// The timer.
        /// </summary>
        Timer timer;

        /// <summary>
        /// The instance.
        /// </summary>
        WinLLDP run;

        /// <summary>
        /// Constructor
        /// </summary>
        public WinLLDPService()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Initialize
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceName = MyServiceName;
            this.CanStop = true;

            this.EventLog.Source = this.ServiceName;
            this.EventLog.Log = "Application";

            if (!EventLog.SourceExists(this.EventLog.Source))
            {
                // Create Windows Event Log source if it doesn't exist
                EventLog.CreateEventSource(this.EventLog.Source, this.EventLog.Log);
            }

            this.EventLog.WriteEntry(string.Format("PowerShell configuration search paths:{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, PowerShellConfigurator.GetPaths().ToArray())), EventLogEntryType.Information);

            try
            {
                string configFile = PowerShellConfigurator.FindConfigurationFile();

                this.EventLog.WriteEntry(string.Format("Using configuration file '{0}'", configFile), EventLogEntryType.Information);

                // Run the service
                this.run = new WinLLDP(configFile);
            }
            catch (PowerShellConfiguratorException e)
            {
                this.EventLog.WriteEntry("Error loading configuration powershell script: " + e, EventLogEntryType.Error);
                throw;
            }

            this.ReduceMemory();

            // Run the LLDP packet sender every 30 seconds
            this.timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds)
            {
                AutoReset = true
            };

            this.timer.Elapsed += this.SendPacket;
        }


        /// <summary>
        /// Main method which is ran every X seconds which is controlled by timer set up in InitializeComponent()  
        /// </summary>
        private void SendPacket(object source, ElapsedEventArgs ea)
        {
            try
            {
                // Run the LLDP packet sender  
                this.run.Run();
                this.ReduceMemory();
            }
            catch (DllNotFoundException ex)
            {
                // Log run error(s) to Windows Event Log
                this.EventLog.WriteEntry("Missing DLL: " + ex, EventLogEntryType.Error);

                throw;
            }
            catch (Exception ex)
            {
                // Log run error(s) to Windows Event Log
                this.EventLog.WriteEntry("Packet sent failed: " + ex, EventLogEntryType.Warning);
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
                this.EventLog.WriteEntry("Freeing memory error: " + ex, EventLogEntryType.Warning);
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
            this.timer.Start();
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            this.timer.Stop();
            this.run.Stop();
        }
    }
}
