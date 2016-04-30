using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Timers;

/// <summary>
/// CLI EXE for quickly debug sent LLDP packets without the need of starting Windows Service
/// </summary>
namespace WinLLDPService.CLI
{
    static class LLDPSender
    {
        private static System.Timers.Timer timer = new System.Timers.Timer(100);
        private static WinLLDP run;

        /// <summary>
        /// Constructor
        /// </summary>
        static void Main(string[] args)
        {
            
            Debug.AutoFlush = true;
            Debug.IndentLevel = 0;

            // Write to CLI also
            TextWriterTraceListener writer = new TextWriterTraceListener(System.Console.Out);
            Debug.Listeners.Add(writer);

            run = new WinLLDP();

            // Send first packet immediately
            sendPacket();

            // Run the LLDP packet sender every X seconds
            Debug.WriteLine("Starting timer", EventLogEntryType.Information);
            timer.Interval = TimeSpan.FromSeconds(10).TotalMilliseconds;
            timer.AutoReset = true;
            timer.Elapsed += triggerEvent;
            timer.Start();

            // Wait for keypress
            Debug.WriteLine("Press key to stop", EventLogEntryType.Information);
            Console.ReadKey();

            timer.Stop();
            
        }

        /// <summary>
        /// Main method which is ran every X seconds which is controlled by timer set up in Main()  
        /// </summary>
        private static void triggerEvent(object source, ElapsedEventArgs ea)
        {
            sendPacket();
        }

        private static void sendPacket() {
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
                Debug.WriteLine("Packet sent failed: " + ex.ToString(), EventLogEntryType.Error);
            }
        }

    }
}
