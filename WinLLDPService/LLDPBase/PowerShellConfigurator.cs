namespace WinLLDPService
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Management.Automation;
    using System.Threading;

    using Microsoft.Win32;

    /// <summary>
    /// Configuration file
    /// <see cref="Configuration"/>
    /// </summary>
    public static class PowerShellConfigurator
    {
        /// <summary>
        /// Get value from registry
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="key">Key</param>
        /// <returns>string value</returns>
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
        /// Get paths where PowerShell configuration file is searched
        /// </summary>
        /// <returns></returns>
        public static List<string> GetPaths()
        {
            List<string> paths = new List<string>
                                     {
                                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinLLDPService"),
                                         Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLLDPService"),
                                     };

            string registrypath = HklmGetString(Path.Combine("Software", "WinLLDPService"), "InstallPath");

            if (!string.IsNullOrEmpty(registrypath) && !paths.Contains(registrypath))
            {
                paths.Add(registrypath);
            }

            string cwd = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);

            if (!string.IsNullOrEmpty(cwd) && !paths.Contains(cwd))
            {
                paths.Add(cwd);
            }

            return paths;
        }

        /// <summary>
        /// Find PowerShell configuration file.
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


            List<string> paths = GetPaths();

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
        /// Load Powershell configuration file.
        /// See: Configuration.default.ps1
        /// </summary>
        /// <returns>
        /// The <see cref="Configuration"/>.
        /// </returns>
        public static Configuration LoadConfiguration(string scriptPath)
        {
            Configuration config = new Configuration();

            using (PowerShell ps = PowerShell.Create())
            {

                // Load DLL so that 
                // "New-Object WinLLDPService.Configuration"
                // can be accessed from configuration file.
                string dll = Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "LLDPBase.dll");

                if (!File.Exists(dll))
                {
                    throw new PowerShellConfiguratorException(string.Format("Configuration file DLL not found: {0}", dll));
                }

                // Load DLL
                ps.AddScript(string.Format("Add-Type -Path '{0}'", dll));

                // Read configuration file
                ps.AddScript(string.Format("& '{0}'", scriptPath));

                PSDataCollection<PSObject> outputCollection = new PSDataCollection<PSObject>();
                IAsyncResult result = ps.BeginInvoke<PSObject, PSObject>(null, outputCollection);
                while (!result.IsCompleted)
                {
                    Thread.Sleep(10);
                }

                if (ps.HadErrors)
                {
                    List<string> errors = new List<string>();
                    foreach (ErrorRecord err in ps.Streams.Error.ReadAll())
                    {
                        errors.Add(err.ToString());
                    }

                    throw new PowerShellConfiguratorException(string.Format("Configuration file error: {0}", string.Join(Environment.NewLine, errors.ToArray())));
                }

                foreach (PSObject outputItem in outputCollection)
                {
                    if (outputItem == null)
                    {
                        throw new NoNullAllowedException();
                    }

                    //Console.WriteLine(outputItem.BaseObject);

                    // Read configuration
                    config = (Configuration)outputItem.BaseObject;
                }
            }

            return config;
        }
    }
}