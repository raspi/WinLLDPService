namespace WinLLDPService
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Management.Automation;
    using System.Threading;
    using System.Timers;

    using Microsoft.Win32;

    public static class PowerShellConfigurator
    {
        public static string HklmGetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);

                if (rk == null)
                {
                    return string.Empty;
                }

                return (string)rk.GetValue(key);
            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// The find configuration file.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string FindConfigurationFile()
        {
            List<string> files = new List<string>
                                     {
                                         "Configuration.ps1",
                                         "Configuration.default.ps1",
                                     };

            List<string> paths = new List<string>
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinLLDPService"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLLDPService"),
                HklmGetString(Path.Combine("Software", "WinLLDPService"), "InstallPath"),
                Directory.GetCurrentDirectory(),
            };

            foreach (string path in paths)
            {
                foreach (string file in files)
                {
                    string f = Path.Combine(path, file);

                    if (File.Exists(f))
                    {
                        return f;
                    }

                }
            }

            throw new PowerShellConfiguratorException(string.Format("Configuration file not found in paths: {0}", string.Join(Environment.NewLine, paths.ToArray())));
        }

        /// <summary>
        /// The load configuration.
        /// </summary>
        /// <returns>
        /// The <see cref="Configuration"/>.
        /// </returns>
        public static Configuration LoadConfiguration(string scriptPath)
        {
            Configuration config = new Configuration();

            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript(string.Format("& '{0}'", scriptPath));

                PSDataCollection<PSObject> outputCollection = new PSDataCollection<PSObject>();
                IAsyncResult result = ps.BeginInvoke<PSObject, PSObject>(null, outputCollection);
                while (!result.IsCompleted)
                {
                    Thread.Sleep(10);
                }

                if (ps.HadErrors)
                {
                    foreach (var err in ps.Streams.Error.ReadAll())
                    {
                        Console.WriteLine(err);
                    }
                }

                foreach (PSObject outputItem in outputCollection)
                {
                    if (outputItem == null)
                    {
                        throw new NoNullAllowedException();
                    }

                    // Read configuration
                    config = (Configuration)outputItem.BaseObject;
                }
            }

            return config;
        }
    }
}