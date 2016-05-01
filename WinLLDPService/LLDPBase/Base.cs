using PacketDotNet;
using PacketDotNet.LLDP;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Win32;

/// <summary>
/// Base classes needed to 
/// - Get adapters
/// - Construct packets
/// - Send packets
/// </summary>
namespace WinLLDPService
{

    public class WinLLDP
    {
        // Uptime
        [System.Runtime.InteropServices.DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        // LLDP MAC address
        readonly static PhysicalAddress destinationHW = new PhysicalAddress(new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e });

        public bool Run()
        {

            Debug.IndentLevel = 1;

            List<NetworkInterface> adapters = new List<NetworkInterface>();

            // Get list of connected adapters
            Debug.WriteLine("Getting connected adapter list", EventLogEntryType.Information);
            adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(
                x =>
                x.NetworkInterfaceType != NetworkInterfaceType.Loopback
                &&
                x.OperationalStatus == OperationalStatus.Up
                )
                .ToList();

            if (0 == adapters.Count)
            {
                Debug.WriteLine("No adapters found.", EventLogEntryType.Information);
                // No available adapters
                return true;
            }

            // Open network devices for sending raw packets
            OpenDevices();

            // Wait to open devices
            System.Threading.Thread.Sleep(Convert.ToInt32(TimeSpan.FromSeconds(1).TotalMilliseconds));

            // Get information which doesn't change often or doesn't need to be 100% accurate in realtime when iterating through adapters
            PacketInfo pinfo = new PacketInfo();
            pinfo.OperatingSystem = FriendlyName().Trim();
            pinfo.OperatingSystemVersion = Environment.OSVersion.ToString().Trim();
            pinfo.Domain = Environment.UserDomainName;
            pinfo.Username = Environment.UserName;
            pinfo.Uptime = GetTickCount64().ToString();


            foreach (NetworkInterface adapter in adapters)
            {
                Debug.WriteLine(String.Format("Adapter {0}:", adapter.Name), EventLogEntryType.Information);

                try
                {
                    Debug.WriteLine("Generating packet", EventLogEntryType.Information);
                    Packet packet = CreateLLDPPacket(adapter, pinfo);
                    Debug.IndentLevel = 1;
                    Debug.WriteLine("Sending packet", EventLogEntryType.Information);
                    sendRawPacket(adapter, packet);
                    Debug.IndentLevel = 1;

                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Format("Error sending packet:{0}{1}", Environment.NewLine, e.ToString()), EventLogEntryType.Error);
                }

                Debug.WriteLine("", EventLogEntryType.Information);
                Debug.WriteLine(new String('-', 40), EventLogEntryType.Information);
                Debug.IndentLevel = 1;
            }

            return true;

        }

        /// <summary>
        /// 
        /// </summary>
        public void Stop()
        {
            CloseDevices();
        }

        /// <summary>
        /// Convert network mask to CIDR notation
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private int getCIDRFromIPAddress(IPAddress ip)
        {
            return Convert.ToString(BitConverter.ToInt32(ip.GetAddressBytes(), 0), 2).ToCharArray().Count(x => x == '1');
        }

        /// <summary>
        /// TLV value's lenght can be only 0-511 octets
        /// Organizational spesific TLV value's lenght can be only 0-507 octets
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private string CreateTlvString(Dictionary<string, string> dict)
        {
            int maxlen = 507;

            string o = String.Empty;

            foreach (var s in dict)
            {
                string tmp = String.Format("{0}:'{1}', ", s.Key, s.Value);

                if ((tmp.Length + o.Length) <= maxlen)
                {
                    o += tmp;
                }
            }

            return o.Trim().TrimEnd(',');
        }

        /// <summary>
        /// Change for example adapter link speed into more human readable format
        /// </summary>
        /// <param name="size"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        static string ReadableSize(double size, int unit = 0)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            while (size >= 1000)
            {
                size /= 1000;
                ++unit;
            }

            return String.Format("{0:G4}{1}", size, units[unit]);
        }

        /// <summary>
        /// Generate LLDP packet for adapter
        /// </summary>
        /// <param name="adapter"></param>
        private Packet CreateLLDPPacket(NetworkInterface adapter, PacketInfo pinfo)
        {
            Debug.IndentLevel = 2;

            PhysicalAddress MACAddress = adapter.GetPhysicalAddress();

            IPInterfaceProperties ipProperties = adapter.GetIPProperties();
            IPv4InterfaceProperties ipv4Properties = null; // Ipv4
            IPv6InterfaceProperties ipv6Properties = null;// Ipv6

            // IPv6
            if (adapter.Supports(NetworkInterfaceComponent.IPv6))
            {
                try
                {
                    ipv6Properties = ipProperties.GetIPv6Properties();
                }
                catch (NetworkInformationException e)
                {
                    // Adapter doesn't probably have IPv6 enabled
                    Debug.WriteLine(e.Message, EventLogEntryType.Warning);
                }
            }

            // IPv4
            if (adapter.Supports(NetworkInterfaceComponent.IPv4))
            {
                try
                {
                    ipv4Properties = ipProperties.GetIPv4Properties();
                }
                catch (NetworkInformationException e)
                {
                    // Adapter doesn't probably have IPv4 enabled
                    Debug.WriteLine(e.Message, EventLogEntryType.Warning);
                }
            }


            // System description
            Dictionary<string, string> systemDescription = new Dictionary<string, string>();
            systemDescription.Add("OS", pinfo.OperatingSystem);
            systemDescription.Add("Ver", pinfo.OperatingSystemVersion);
            systemDescription.Add("User", pinfo.Username);
            systemDescription.Add("Domain", pinfo.Domain);
            systemDescription.Add("Uptime", pinfo.Uptime);

            // Port description
            Dictionary<string, string> portDescription = new Dictionary<string, string>();

            // adapter.Description is for example "Intel(R) 82579V Gigabit Network Connection"
            // Registry: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkCards\<number>\Description value
            portDescription.Add("Vendor", adapter.Description);

            /*
             adapter.Id is GUID and can be found in several places: 
             In this example it is "{87423023-7191-4C03-A049-B8E7DBB36DA4}"
            
             Registry: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion
             - \NetworkCards\<number>\ServiceName value (in same tree as adapter.Description!)
             - \NetworkList\Nla\Cache\Intranet\<adapter.Id> key
             - \NetworkList\Nla\Cache\Intranet\<domain>\<adapter.Id> key
            
             Registry: HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion
             - \NetworkCards\<number>\ServiceName value
            
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4D36E972-E325-11CE-BFC1-08002BE10318}\0007
               {4D36E972-E325-11CE-BFC1-08002BE10318} == Network and Sharing Center -> viewing adapter's "Properties" and selecting "Device class GUID" from dropdown menu
               {4D36E972-E325-11CE-BFC1-08002BE10318}\0007 == Network and Sharing Center -> viewing adapter's "Properties" and selecting "Driver key" from dropdown menu
             - \NetCfgInstanceId value
             - \Linkage\Export value (part of)
             - \Linkage\FilterList value (part of)
             - \Linkage\RootDevice value 
            
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceClasses\{ad498944-762f-11d0-8dcb-00c04fc3358c}\##?#PCI#VEN_8086&DEV_1503&SUBSYS_849C1043&REV_06#3&11583659&0&C8#{ad498944-762f-11d0-8dcb-00c04fc3358c}\#{87423023-7191-4C03-A049-B8E7DBB36DA4}\SymbolicLink value (part of)
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Network\{ 4D36E972 - E325 - 11CE - BFC1 - 08002BE10318}\{ 87423023 - 7191 - 4C03 - A049 - B8E7DBB36DA4}
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Network\{4D36E972-E325-11CE-BFC1-08002BE10318}\{ACB3F7A0-2E45-4435-854A-A4E120477E1D}\Connection\Name value (part of)
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\{ 87423023 - 7191 - 4C03 - A049 - B8E7DBB36DA4}
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\iphlpsvc\Parameters\Isatap\{ ACB3F7A0 - 2E45 - 4435 - 854A - A4E120477E1D}\InterfaceName value (part of)
            
             Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\<various names>\Linkage
             - \Bind value (part of)
             - \Export value (part of)
             - \Route value (part of)
            
            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\NetBT\Parameters\Interfaces\Tcpip_{87423023-7191-4C03-A049-B8E7DBB36DA4}
            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Psched\Parameters\NdisAdapters\{87423023-7191-4C03-A049-B8E7DBB36DA4}
            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\RemoteAccess\Interfaces\<number>\InterfaceName value
            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Tcpip\Parameters\Adapters\{87423023-7191-4C03-A049-B8E7DBB36DA4}\IpConfig value (part of)

            IPv4 information:
            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\Tcpip\Parameters\Interfaces\{87423023-7191-4C03-A049-B8E7DBB36DA4}

            IPv6 information:
            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\TCPIP6\Parameters\Interfaces\{87423023-7191-4c03-a049-b8e7dbb36da4}

            Registry: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\services\WfpLwf\Parameters\NdisAdapters\{87423023-7191-4C03-A049-B8E7DBB36DA4}

            */
            portDescription.Add("ID", adapter.Id);

            // Gateway
            if (ipProperties.GatewayAddresses.Count > 0)
            {
                portDescription.Add("GW", String.Join(", ", ipProperties.GatewayAddresses.Select(i => i.Address.ToString()).ToArray()));
            }
            else
            {
                portDescription.Add("GW", "-");
            }

            // CIDR
            if (ipProperties.UnicastAddresses.Count > 0)
            {
                int[] mask = ipProperties.UnicastAddresses
                    .Where(
                      w => w.IPv4Mask.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    )
                    .Select(x => getCIDRFromIPAddress(x.IPv4Mask))
                    .ToArray()
                    ;


                portDescription.Add("CIDR", String.Join(", ", mask));
            }
            else
            {
                portDescription.Add("CIDR", "-");
            }


            // DNS server(s)
            if (ipProperties.DnsAddresses.Count > 0)
            {
                portDescription.Add("DNS", String.Join(", ", ipProperties.DnsAddresses.Select(i => i.ToString()).ToArray()));
            }
            else
            {
                portDescription.Add("DNS", "-");
            }

            // DHCP server
            if (ipProperties.DhcpServerAddresses.Count > 0)
            {
                portDescription.Add("DHCP", String.Join(", ", ipProperties.DhcpServerAddresses.Select(i => i.ToString()).ToArray()));
            }
            else
            {
                portDescription.Add("DHCP", "-");
            }

            // WINS server(s)
            if (ipProperties.WinsServersAddresses.Count > 0)
            {
                portDescription.Add("WINS", String.Join(", ", ipProperties.WinsServersAddresses.Select(i => i.ToString()).ToArray()));
            }


            // Link speed
            portDescription.Add("Speed", ReadableSize(adapter.Speed) + "ps");

            // Capabilities enabled
            List<CapabilityOptions> capabilitiesEnabled = new List<CapabilityOptions>();
            capabilitiesEnabled.Add(CapabilityOptions.StationOnly);

            if (ipv4Properties.IsForwardingEnabled)
            {
                capabilitiesEnabled.Add(CapabilityOptions.Router);
            }

            ushort expectedSystemCapabilitiesCapability = GetCapabilityOptionsBits(GetCapabilityOptions());
            ushort expectedSystemCapabilitiesEnabled = GetCapabilityOptionsBits(capabilitiesEnabled);

            // Constuct LLDP packet 
            LLDPPacket lldpPacket = new LLDPPacket();
            lldpPacket.TlvCollection.Add(new ChassisID(ChassisSubTypes.MACAddress, MACAddress));
            lldpPacket.TlvCollection.Add(new PortID(PortSubTypes.LocallyAssigned, System.Text.Encoding.UTF8.GetBytes(adapter.Name)));
            lldpPacket.TlvCollection.Add(new TimeToLive(120));
            lldpPacket.TlvCollection.Add(new PortDescription(CreateTlvString(portDescription)));
            lldpPacket.TlvCollection.Add(new SystemName(Environment.MachineName));
            lldpPacket.TlvCollection.Add(new SystemDescription(CreateTlvString(systemDescription)));
            lldpPacket.TlvCollection.Add(new SystemCapabilities(expectedSystemCapabilitiesCapability, expectedSystemCapabilitiesEnabled));

            // Management
            var managementAddressObjectIdentifier = "Management";

            // Add management IPv4 address(es)
            if (null != ipv4Properties)
            {
                foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        lldpPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ip.Address), InterfaceNumbering.SystemPortNumber, Convert.ToUInt32(ipv4Properties.Index), managementAddressObjectIdentifier));
                    }
                }
            }

            // Add management IPv6 address(es)
            if (null != ipv6Properties)
            {
                foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        lldpPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ip.Address), InterfaceNumbering.SystemPortNumber, Convert.ToUInt32(ipv6Properties.Index), managementAddressObjectIdentifier));
                    }
                }
            }

            // == Organization specific TLVs

            // Ethernet
            lldpPacket.TlvCollection.Add(new OrganizationSpecific(new byte[] { 0x0, 0x12, 0x0f }, 5, new byte[] { 0x5 }));

            var expectedOrganizationUniqueIdentifier = new byte[3] { 0, 0, 0 };
            var expectedOrganizationSpecificBytes = new byte[] { 0, 0, 0, 0 };

            //int orgSubType = 0;

            // IPv4 Information:
            //if (null != ipv4Properties)
            //{
            //    lldpPacket.TlvCollection.Add((new StringTLV(TLVTypes.OrganizationSpecific, String.Format("IPv4 DHCP: {0}", ipv4Properties.IsDhcpEnabled.ToString()))));
            //lldpPacket.TlvCollection.Add(new OrganizationSpecific(expectedOrganizationSpecificBytes, new StringTLV(), System.Text.Encoding.UTF8.GetBytes(String.Format("IPv4 DHCP: {0}", ipv4Properties.IsDhcpEnabled.ToString()))));
            //}

            // End of LLDP packet
            lldpPacket.TlvCollection.Add(new EndOfLLDPDU());

            if (0 == lldpPacket.TlvCollection.Count)
            {
                throw new Exception("Couldn't construct LLDP TLVs.");
            }

            if (lldpPacket.TlvCollection.Last().GetType() != typeof(EndOfLLDPDU))
            {
                throw new Exception("Last TLV must be type of 'EndOfLLDPDU'!");
            }

            foreach (TLV tlv in lldpPacket.TlvCollection)
            {
                Debug.WriteLine(tlv.ToString(), EventLogEntryType.Information);
            }

            // Generate packet
            Packet packet = new EthernetPacket(MACAddress, destinationHW, EthernetPacketType.LLDP);
            packet.PayloadData = lldpPacket.Bytes;

            return packet;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adapter"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        private bool sendRawPacket(NetworkInterface adapter, Packet payload)
        {
            Debug.WriteLine("Sending RAW packet", EventLogEntryType.Information);

            foreach (PcapDevice device in CaptureDeviceList.Instance)
            {
                if (adapter.GetPhysicalAddress().Equals(device.MacAddress))
                {
                    Debug.WriteLine("Device found!", EventLogEntryType.Information);


                    if (!device.Opened)
                    {
                        Debug.WriteLine("Device is not open.", EventLogEntryType.Error);
                        return false;
                    }

                    device.SendPacket(payload);

                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// Open devices for sending
        /// </summary>
        /// <param name="adapters"></param>
        private void OpenDevices()
        {
            Debug.WriteLine("Opening devices..", EventLogEntryType.Information);

            foreach (PcapDevice device in CaptureDeviceList.Instance)
            {
                if (!device.Opened)
                {
                    device.Open(DeviceMode.Promiscuous);
                }
            }

        }

        /// <summary>
        /// Close devices for sending
        /// </summary>
        /// <param name="adapters"></param>
        private void CloseDevices()
        {
            Debug.WriteLine("Closing devices..", EventLogEntryType.Information);

            foreach (PcapDevice device in CaptureDeviceList.Instance)
            {
                if (device.Opened)
                {
                    device.Close();
                }
            }

        }



        /// <summary>
        /// List possible LLDP capabilities such as:
        /// - Bridge 
        /// - Router 
        /// - WLAN Access Point 
        /// - Station 
        /// </summary>
        /// <returns>List<CapabilityOptions></returns>
        private List<CapabilityOptions> GetCapabilityOptions()
        {
            List<CapabilityOptions> capabilities = new List<CapabilityOptions>();
            capabilities.Add(CapabilityOptions.Bridge);
            capabilities.Add(CapabilityOptions.Router);
            capabilities.Add(CapabilityOptions.WLanAP);
            capabilities.Add(CapabilityOptions.StationOnly);

            return capabilities;
        }

        /// <summary>
        /// Get LLDP capabilities as bits
        /// </summary>
        /// <returns>ushort</returns>
        private ushort GetCapabilityOptionsBits(List<CapabilityOptions> capabilities)
        {
            ushort caps = 0;

            foreach (var cap in capabilities)
            {
                ushort tmp = ushort.Parse(cap.GetHashCode().ToString());
                caps |= tmp;
            }

            return caps;
        }

        public string HKLM_GetString(string path, string key)
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
        /// Get Windows "friendly name" for example "Windows 7 Ultimate edition"
        /// </summary>
        /// <returns></returns>
        public string FriendlyName()
        {
            string ProductName = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName");
            string CSDVersion = HKLM_GetString(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CSDVersion");

            if (!String.IsNullOrEmpty(ProductName))
            {
                return (ProductName.StartsWith("Microsoft") ? "" : "Microsoft ") + ProductName +
                            (CSDVersion != "" ? " " + CSDVersion : "");
            }

            return "?";
        }

    }
}