namespace WinLLDPService.CLI
{
    using System;
    using System.Diagnostics;
    using System.Timers;

    /// <summary>
    /// CLI EXE for quickly debug sent LLDP packets without the need of starting Windows Service
    /// </summary>
    static class LLDPSender
    {
        /// <summary>
        /// The timer.
        /// </summary>
        private static Timer timer = new Timer(100);

        /// <summary>
        /// The WinLLDP instance.
        /// </summary>
        private static WinLLDP run;

        /// <summary>
        /// Constructor for sender
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {

            Debug.AutoFlush = true;
            Debug.IndentLevel = 0;

            // Write to CLI also
            TextWriterTraceListener writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);

            run = new WinLLDP(OsInfo.GetStaticInfo());

            // Send first packet immediately
            SendPacket();

            // Run the LLDP packet sender every X seconds
            Debug.WriteLine("Starting timer", EventLogEntryType.Information);
            timer.Interval = TimeSpan.FromSeconds(10).TotalMilliseconds;
            timer.AutoReset = true;
            timer.Elapsed += TriggerEvent;
            timer.Start();

            // Wait for keypress
            Debug.WriteLine("Press key to stop", EventLogEntryType.Information);
            Console.ReadKey();

            timer.Stop();

        }

        /// <summary>
        /// Main method which is ran every X seconds which is controlled by timer set up in Main()  
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="ea">
        /// The ea.
        /// </param>
        private static void TriggerEvent(object source, ElapsedEventArgs ea)
        {
            SendPacket();
        }

        /// <summary>
        /// The send packet.
        /// </summary>
        private static void SendPacket()
        {
            Debug.IndentLevel = 0;

            try
            {
                // Run the LLDP packet sender  
                Debug.WriteLine("Sending LLDP packet..", EventLogEntryType.Information);
                run.Run();
                Debug.WriteLine("Packet sent.", EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Packet sent failed: " + ex, EventLogEntryType.Error);
            }
        }

    }
}
