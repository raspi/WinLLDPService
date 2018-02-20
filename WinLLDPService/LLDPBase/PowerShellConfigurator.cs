namespace WinLLDPService
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Threading;
    using System.Threading.Tasks;
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
        /// <param name="scriptPath">
        /// The PowerShell script path.
        /// </param>
        /// <returns>
        /// The <see cref="Configuration"/>.
        /// </returns>
        public static async Task<Configuration> LoadConfiguration(string scriptPath)
        {

            if (!File.Exists(scriptPath))
            {
                throw new PowerShellConfiguratorException(string.Format("File not found: {0}", scriptPath));
            }

            // Load default
            Configuration defaultConfig = new Configuration();

            InitialSessionState initial = InitialSessionState.CreateDefault();
            initial.LanguageMode = PSLanguageMode.RestrictedLanguage;
            initial.ApartmentState = ApartmentState.STA;
            initial.ThrowOnRunspaceOpenError = true;

            // Load DLL so that 
            // "New-Object WinLLDPService.Configuration"
            // can be accessed from configuration file.
            string dll = Path.Combine(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory), "LLDPBase.dll");

            if (!File.Exists(dll))
            {
                throw new PowerShellConfiguratorException(string.Format("Configuration file DLL not found: {0}", dll));
            }

            // Load DLL
            initial.ImportPSModule(new string[] { dll });

            using (Runspace rs = RunspaceFactory.CreateRunspace(initial))
            using (PowerShell ps = PowerShell.Create())
            {
                rs.Open();
                ps.Runspace = rs;

                // Read configuration file
                ps.AddScript(string.Format("& '{0}'", scriptPath));

                PSDataCollection<PSObject> outputCollection = await Task<PSDataCollection<PSObject>>.Factory.FromAsync(ps.BeginInvoke(), ps.EndInvoke);

                if (ps.HadErrors)
                {
                    List<string> errors = new List<string>();
                    foreach (ErrorRecord err in ps.Streams.Error.ReadAll())
                    {
                        errors.Add(err.ToString());
                    }

                    throw new PowerShellConfiguratorException(string.Format("Configuration file error: {0}", string.Join(Environment.NewLine, errors.ToArray())));
                }

                Configuration cfg = outputCollection.FirstOrDefault(x => x.BaseObject is Configuration)?.BaseObject as Configuration;

                if (cfg != null)
                {
                    return cfg;
                }
            }

            // Return default
            return defaultConfig;
        }
    }
}