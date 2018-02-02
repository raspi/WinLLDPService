namespace WinLLDPService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Management;

    using Microsoft.Win32;

    /// <summary>
    /// The os info.
    /// </summary>
    public class OsInfo
    {
        /// <summary>
        /// The hkl m_ get string.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
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
        /// Shorten the operating system name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ReplaceName(string name)
        {
            return name
                .Replace("Microsoft", string.Empty)
                .Replace("Windows", "Win")
                .Replace("Professional", "Pro")
                .Replace("Ultimate", "Ult")
                .Replace("Enterprise", "Ent")
                .Replace("Edition", "Ed")
                .Replace("Service Pack", "SP")
            ;

        }

        /// <summary>
        /// Get OS "friendly name" for example "Windows 7 Ultimate edition"
        /// </summary>
        /// <returns></returns>
        public static string FriendlyName()
        {
            string name = "?";

            string productName = HklmGetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string csdVersion = HklmGetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");

            if (!string.IsNullOrEmpty(productName))
            {
                name = (productName.StartsWith("Microsoft") ? string.Empty : "Microsoft ") + productName + (csdVersion != string.Empty ? " " + csdVersion : string.Empty);
            }

            return name.Trim();
        }

        /// <summary>
        /// Uptime as string
        /// </summary>
        /// <returns></returns>
        public static string GetUptime()
        {
            TimeSpan uptime = TimeSpan.FromMilliseconds(NativeMethods.GetTickCount64());
            return string.Format("{0:000}d{1:00}h{2:00}m{3:00}s", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds);
        }

        /// <summary>
        /// Get list of users
        /// 
        /// We cant use Environment.UserName or System.Security.Principal.WindowsIdentity.GetCurrent().Name 
        /// because those return ("SYSTEM" or "NT AUTHORITY\SYSTEM").
        /// This is because it is reading Windows Service user, not logged in user(s).
        /// 
        /// So different approach finding correct logged in user(s) have to be used.
        /// </summary>
        /// <returns></returns>
        public static List<UserInfo> GetUsers()
        {
            List<UserInfo> users = new List<UserInfo>();

            string query = "SELECT * FROM Win32_Process WHERE ProcessID = '{0}'";

            // Find all explorer.exe processes
            foreach (Process p in Process.GetProcessesByName("explorer"))
            {
                foreach (ManagementObject mo in new ManagementObjectSearcher(new ObjectQuery(string.Format(query, p.Id))).Get())
                {
                    string[] s = new string[2];
                    mo.InvokeMethod("GetOwner", s);

                    string user = s[0];
                    string domain = s[1];

                    UserInfo ui = new UserInfo
                                      {
                        Username = user,
                        Domain = domain
                    };

                    // Add to list
                    if (!users.Any(x => x.Domain == domain && x.Username == user))
                    {
                        users.Add(ui);
                    }
                }
            }

            return users.OrderBy(x => x.Domain).ThenBy(x => x.Username).ToList();
        }

        public static string GetServiceTag()
        {
            string tag = string.Empty;

            foreach (ManagementObject obj in new ManagementObjectSearcher(new SelectQuery("Win32_Bios")).Get())
            {
                tag = obj.Properties["Serialnumber"].Value.ToString().Trim();
                if (tag.Length > 3)
                {
                    break;
                }
            }

            if (tag.ToLower().StartsWith("vmware"))
            {
                tag = "vmware";
            }

            string[] invalid =
            {
                "serial",
                "oem",
                "o.e.m.",
                "n/a",
                "na"
            };

            if (!invalid.Any(tag.ToLower().Contains))
            {
                return tag;
            }

            // Use CPU ID instead
            foreach (ManagementObject obj in new ManagementObjectSearcher(new SelectQuery("Win32_Processor")).Get())
            {
                tag = "CPU-" + obj.Properties["ProcessorId"].Value.ToString().Trim();
                break;
            }

            return tag;

        }

        /// <summary>
        /// Static information which isn't changed after boot
        /// </summary>
        /// <returns></returns>
        public static StaticInfo GetStaticInfo()
        {
            StaticInfo si = new StaticInfo
                                {
                OperatingSystemFriendlyName = ReplaceName(FriendlyName()).Trim(),
                OperatingSystemVersion = ReplaceName(Environment.OSVersion.ToString().Trim()),
                ServiceTag = GetServiceTag(),
                MachineName = Environment.MachineName
            };

            return si;

        }
    }
}