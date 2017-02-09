using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace WinLLDPService
{

    public class OsInfo
    {
        public static string HKLM_GetString(string path, string key)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine.OpenSubKey(path);
                if (rk == null) return "";
                return (string)rk.GetValue(key);
            }
            catch
            {
            }

            return "";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ReplaceName(string name)
        {
            return name
                .Replace("Microsoft", "")
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
            string Name = "?";

            string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");

            if (!String.IsNullOrEmpty(ProductName))
            {
                Name = (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName + (CSDVersion != "" ? " " + CSDVersion : "");
            }

            return Name.Trim();
        }

        /// <summary>
        /// Uptime as string
        /// </summary>
        /// <returns></returns>
        public static string GetUptime()
        {
            TimeSpan uptime = TimeSpan.FromMilliseconds(NativeMethods.GetTickCount64());
            return String.Format("{0:000}d{1:00}h{2:00}m{3:00}s", uptime.Days, uptime.Hours, uptime.Minutes, uptime.Seconds);
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
                foreach (ManagementObject mo in new ManagementObjectSearcher(new ObjectQuery(String.Format(query, p.Id))).Get())
                {
                    string[] s = new string[2];
                    mo.InvokeMethod("GetOwner", (object[])s);

                    string user = s[0].ToString();
                    string domain = s[1].ToString();

                    UserInfo ui = new UserInfo()
                    {
                        Username = user,
                        Domain = domain,
                    };

                    if (users.Where(x => x.Domain == domain && x.Username == user).Count() == 0)
                    {
                        users.Add(ui);
                    }

                }

            }

            return users.OrderBy(x => x.Domain).ThenBy(x => x.Username).ToList();

        }

        public static string GetServiceTag()
        {
            string tag = "";

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
                "na",
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
            StaticInfo si = new StaticInfo()
            {
                OperatingSystemFriendlyName = ReplaceName(FriendlyName()).Trim(),
                OperatingSystemVersion = ReplaceName(Environment.OSVersion.ToString().Trim()),
                ServiceTag = GetServiceTag(),
                MachineName = Environment.MachineName,
            };

            return si;

        }
    }
}