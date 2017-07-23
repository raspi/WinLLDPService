using PacketDotNet;
using PacketDotNet.LLDP;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;

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
        StaticInfo StaticInformation = new StaticInfo();

        public WinLLDP(StaticInfo si)
        {
            StaticInformation = si;
        }

        // LLDP MAC address
        readonly static PhysicalAddress LldpDestinationMacAddress = new PhysicalAddress(new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e });

        public bool Run()
        {

            Debug.IndentLevel = 0;

            List<NetworkInterface> adapters = new List<NetworkInterface>();

            // Get list of connected adapters
            Debug.WriteLine("Getting connected adapter list..", EventLogEntryType.Information);
            adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(
                  x =>
                  // Link is up
                  x.OperationalStatus == OperationalStatus.Up
                  &&
                  // Not loopback (127.0.0.1 / ::1)
                  x.NetworkInterfaceType != NetworkInterfaceType.Loopback
                  &&
                  // Not tunnel
                  x.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                  &&
                  // Supports IPv4 or IPv6
                  (x.Supports(NetworkInterfaceComponent.IPv4) || x.Supports(NetworkInterfaceComponent.IPv6))
                )
                .ToList();

            if (0 == adapters.Count)
            {
                Debug.WriteLine("No adapters found.", EventLogEntryType.Information);
                // No available adapters
                return true;
            }

            // Open network devices for sending raw packets
            OpenDevices(adapters);

            // Get list of users
            List<UserInfo> users = OsInfo.GetUsers();

            if (users.Count == 0)
            {
                Debug.WriteLine("No users found.", EventLogEntryType.Error);
            }

            // Get information which doesn't change often or doesn't need to be 100% accurate in realtime when iterating through adapters
            PacketInfo pinfo = new PacketInfo()
            {
                OperatingSystem = StaticInformation.OperatingSystemFriendlyName,
                OperatingSystemVersion = StaticInformation.OperatingSystemVersion,
                Tag = StaticInformation.ServiceTag,
                Uptime = OsInfo.GetUptime(),
                MachineName = StaticInformation.MachineName,
            };

            bool first = true;
            string olddomain = "";
            string userlist = "";

            foreach (UserInfo ui in users)
            {
                string domain = ui.Domain;

                if (domain != olddomain)
                {
                    first = true;
                }

                if (first)
                {
                    userlist += domain + ": ";
                    olddomain = domain;
                    first = false;
                }

                userlist += ui.Username + ", ";

            }

            pinfo.Username = userlist.Trim().TrimEnd(',');

            foreach (NetworkInterface adapter in adapters)
            {
                Debug.WriteLine(String.Format("Adapter {0}:", adapter.Name), EventLogEntryType.Information);

                try
                {
                    Debug.WriteLine("Generating packet", EventLogEntryType.Information);
                    Packet packet = CreateLLDPPacket(adapter, pinfo);
                    Debug.IndentLevel = 0;
                    Debug.WriteLine("Sending packet", EventLogEntryType.Information);
                    SendRawPacket(adapter, packet);
                    Debug.IndentLevel = 0;

                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Format("Error sending packet:{0}{1}", Environment.NewLine, e.ToString()), EventLogEntryType.Error);
                }

                Debug.WriteLine("", EventLogEntryType.Information);
                Debug.WriteLine(new String('-', 40), EventLogEntryType.Information);
                Debug.IndentLevel = 0;
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
        /// TLV value's length can be only 0-511 bytes
        /// Organizational spesific TLV value's length can be only 0-507 bytes
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private string CreateTlvString(Dictionary<string, string> dict)
        {
            int maxlen = 507;

            string o = String.Empty;

            foreach (var s in dict)
            {
                string tmp = String.Format("{0}:{1} | ", s.Key, s.Value);

                if ((tmp.Length + o.Length) <= maxlen)
                {
                    o += tmp;
                }
                else
                {
                    Debug.WriteLine("Data doesn't fit to string", EventLogEntryType.Warning);
                }
            }

            o = o.Trim().TrimEnd('|');

            Debug.WriteLine(o);

            return o;
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
            IPv6InterfaceProperties ipv6Properties = null; // Ipv6

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
            Dictionary<string, string> systemDescription = new Dictionary<string, string>
            {
                { "OS", pinfo.OperatingSystem },
                { "Id", pinfo.Tag },
                { "U", pinfo.Username },
                { "Up", pinfo.Uptime },
                { "Ver", pinfo.OperatingSystemVersion },
            };

            // Port description
            Dictionary<string, string> portDescription = new Dictionary<string, string>
            {
            };

            // Gateway
            if (ipProperties.GatewayAddresses.Count > 0)
            {
                portDescription.Add("GW", String.Join(", ", ipProperties.GatewayAddresses.Select(i => i.Address.ToString()).ToArray()).Trim().Trim(','));
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
                    .Select(x => NetworkInfo.GetCIDRFromIPMaskAddress(x.IPv4Mask))
                    .ToArray()
                    ;

                portDescription.Add("NM", String.Join(", ", mask).Trim().Trim(','));
            }
            else
            {
                portDescription.Add("NM", "-");
            }


            // DNS server(s)
            if (ipProperties.DnsAddresses.Count > 0)
            {
                portDescription.Add("DNS", String.Join(", ", ipProperties.DnsAddresses.Select(i => i.ToString()).ToArray()).Trim().Trim(','));
            }
            else
            {
                portDescription.Add("DNS", "-");
            }

            // DHCP server
            if (ipProperties.DhcpServerAddresses.Count > 0)
            {
                portDescription.Add("DHCP", String.Join(", ", ipProperties.DhcpServerAddresses.Select(i => i.ToString()).ToArray()).Trim().Trim(','));
            }
            else
            {
                portDescription.Add("DHCP", "-");
            }

            // WINS server(s)
            if (ipProperties.WinsServersAddresses.Count > 0)
            {
                portDescription.Add("WINS", String.Join(", ", ipProperties.WinsServersAddresses.Select(i => i.ToString()).ToArray()).Trim().Trim(','));
            }


            // Link speed
            portDescription.Add("Spd", NetworkInfo.ReadableSize(adapter.Speed));

            // adapter.Description is for example "Intel(R) 82579V Gigabit Network Connection"
            // Registry: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkCards\<number>\Description value
            portDescription.Add("Vendor", adapter.Description);

            // Capabilities enabled
            List<CapabilityOptions> capabilitiesEnabled = new List<CapabilityOptions>
            {
                CapabilityOptions.StationOnly
            };

            ushort expectedSystemCapabilitiesCapability = GetCapabilityOptionsBits(GetCapabilityOptions(adapter));
            ushort expectedSystemCapabilitiesEnabled = GetCapabilityOptionsBits(capabilitiesEnabled);

            // Constuct LLDP packet 
            LLDPPacket lldpPacket = new LLDPPacket();
            lldpPacket.TlvCollection.Add(new ChassisID(ChassisSubTypes.MACAddress, MACAddress));
            lldpPacket.TlvCollection.Add(new PortID(PortSubTypes.LocallyAssigned, System.Text.Encoding.UTF8.GetBytes(adapter.Name)));
            lldpPacket.TlvCollection.Add(new TimeToLive(120));
            lldpPacket.TlvCollection.Add(new PortDescription(CreateTlvString(portDescription)));

            lldpPacket.TlvCollection.Add(new SystemName(pinfo.MachineName));
            lldpPacket.TlvCollection.Add(new SystemDescription(CreateTlvString(systemDescription)));
            lldpPacket.TlvCollection.Add(new SystemCapabilities(expectedSystemCapabilitiesCapability, expectedSystemCapabilitiesEnabled));

            // Add management address(es)
            if (null != ipv4Properties)
            {
                if (ipv4Properties.IsForwardingEnabled)
                {
                    capabilitiesEnabled.Add(CapabilityOptions.Router);
                }

                // Add management IPv4 address(es)
                foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        lldpPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ip.Address), InterfaceNumbering.SystemPortNumber, Convert.ToUInt32(ipv4Properties.Index), ""));
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
                        lldpPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ip.Address), InterfaceNumbering.SystemPortNumber, Convert.ToUInt32(ipv6Properties.Index), ""));
                    }
                }
            }

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

            // Generate packet
            Packet packet = new EthernetPacket(MACAddress, LldpDestinationMacAddress, EthernetPacketType.LLDP)
            {
                PayloadData = lldpPacket.Bytes
            };

            return packet;

        }

        /// <summary>
        /// Send packet using SharpPcap
        /// </summary>
        /// <param name="adapter">Network adapter</param>
        /// <param name="payload">Packet to be sent</param>
        /// <returns>bool</returns>
        private bool SendRawPacket(NetworkInterface adapter, Packet payload)
        {
            Debug.WriteLine("Sending RAW packet", EventLogEntryType.Information);

            PcapDevice device = (PcapDevice)CaptureDeviceList.Instance.Where(x => x.MacAddress.Equals(adapter.GetPhysicalAddress())).First();

            if (device.Opened)
            {
                device.SendPacket(payload);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Open selected network devices for sending
        /// </summary>
        /// <param name="adapters"></param>
        private void OpenDevices(List<NetworkInterface> adapters)
        {
            Debug.WriteLine("Opening devices..", EventLogEntryType.Information);

            // TODO: select only devices that are on adapters list
            CaptureDeviceList selected = CaptureDeviceList.Instance;

            if (0 == selected.Count())
            {
                Debug.WriteLine("No adapters found.");
                return;
            }

            // Wait time in milliseconds
            int waitTime = 2000;

            // Open selected adapters
            foreach (PcapDevice device in selected)
            {
                device.Open(DeviceMode.Promiscuous, waitTime);
            }

            // Wait adapters to open
            foreach (PcapDevice device in selected)
            {
                bool wait = true;

                Stopwatch sw = new Stopwatch();
                sw.Start();

                do
                {
                    // Check that adapter has opened or timer has elapsed
                    if (device.Opened || sw.ElapsedMilliseconds >= waitTime)
                    {
                        wait = false;
                        break;
                    }

                    // Sleep
                    System.Threading.Thread.Sleep(15);

                } while (wait);

                sw.Stop();
                
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
        private List<CapabilityOptions> GetCapabilityOptions(NetworkInterface adapter)
        {
            List<CapabilityOptions> capabilities = new List<CapabilityOptions>
            {
                CapabilityOptions.Bridge,
                CapabilityOptions.Router,
                CapabilityOptions.StationOnly
            };

            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
            {
                capabilities.Add(CapabilityOptions.WLanAP);
            }

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

    }
}