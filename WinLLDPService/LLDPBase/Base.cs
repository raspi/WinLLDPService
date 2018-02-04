namespace WinLLDPService
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading;

    using PacketDotNet;
    using PacketDotNet.LLDP;

    using SharpPcap;
    using SharpPcap.LibPcap;

    using AddressFamily = System.Net.Sockets.AddressFamily;

    /// <summary>
    /// Base classes needed to 
    /// - Get adapters
    /// - Construct packets
    /// - Send packets
    /// </summary>
    public class WinLLDP
    {
        /// <summary>
        /// Gets or sets the static information.
        /// </summary>
        public StaticInfo StaticInformation { get; set; }

        /// <summary>
        /// The LLDP destination mac address.
        /// </summary>
        public static readonly PhysicalAddress LldpDestinationMacAddress = new PhysicalAddress(new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e });

        /// <summary>
        /// Set static information
        /// </summary>
        /// <param name="si"></param>
        public WinLLDP(StaticInfo si)
        {
            this.StaticInformation = si;
        }

        /// <summary>
        /// The run.
        /// </summary>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Run()
        {
            Debug.IndentLevel = 0;

            // Get list of connected adapters
            Debug.WriteLine("Getting connected adapter list..", EventLogEntryType.Information);
            List<NetworkInterface> adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(
                    x =>

                            // Link is up
                            x.OperationalStatus == OperationalStatus.Up &&

                            // Not loopback (127.0.0.1 / ::1)
                            x.NetworkInterfaceType != NetworkInterfaceType.Loopback &&

                            // Not tunnel
                            x.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&

                            // Supports IPv4 or IPv6
                            (x.Supports(NetworkInterfaceComponent.IPv4) || x.Supports(NetworkInterfaceComponent.IPv6)))
                .ToList();

            if (!adapters.Any())
            {
                Debug.WriteLine("No adapters found.", EventLogEntryType.Information);

                // No available adapters
                return true;
            }

            // Get list of users
            List<UserInfo> users = OsInfo.GetUsers();

            if (users.Count == 0)
            {
                Debug.WriteLine("No users found.", EventLogEntryType.Error);
            }

            // Get information which doesn't change often or doesn't need to be 100% accurate in realtime when iterating through adapters
            PacketInfo pinfo = new PacketInfo
            {
                OperatingSystem = this.StaticInformation.OperatingSystemFriendlyName,
                OperatingSystemVersion = this.StaticInformation.OperatingSystemVersion,
                Tag = this.StaticInformation.ServiceTag,
                Uptime = OsInfo.GetUptime(),
                MachineName = this.StaticInformation.MachineName
            };

            bool first = true;
            string olddomain = string.Empty;
            string userlist = string.Empty;

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

            CaptureDeviceList devices = CaptureDeviceList.Instance;

            // Wait time in milliseconds
            int waitTime = 10000;
            Stopwatch sw = new Stopwatch();

            foreach (NetworkInterface adapter in adapters)
            {
                sw.Reset();

                Debug.WriteLine(string.Format("Adapter {0}:", adapter.Name), EventLogEntryType.Information);

                try
                {
                    Debug.WriteLine("Generating packet", EventLogEntryType.Information);
                    Packet packet = this.CreateLLDPPacket(adapter, pinfo);
                    Debug.IndentLevel = 0;
                    Debug.WriteLine("Sending packet", EventLogEntryType.Information);

                    PcapDevice device = null;

                    for (var index = 0; index < devices.Count; index++)
                    {
                        PcapDevice dev = (PcapDevice)devices[index];
                        PcapInterface iface = dev.Interface;

                        if (iface.MacAddress.Equals(adapter.GetPhysicalAddress()))
                        {
                            device = dev;
                            break;
                        }
                    }

                    if (device == null)
                    {
                        continue;
                    }

                    device.Open(DeviceMode.Promiscuous, waitTime);

                    sw.Start();
                    do
                    {
                        // Sleep
                        Thread.Sleep(15);
                    }
                    while (!device.Opened || sw.ElapsedMilliseconds >= waitTime);

                    sw.Stop();

                    // Send packet
                    if (!this.SendRawPacket(device, packet))
                    {
                        Debug.WriteLine("Sending failed", EventLogEntryType.Information);
                    }

                    Debug.IndentLevel = 0;

                }
                catch (PcapException e)
                {
                    Debug.WriteLine(string.Format("Error sending packet:{0}{1}", Environment.NewLine, e), EventLogEntryType.Error);
                }

                Debug.WriteLine(string.Empty, EventLogEntryType.Information);
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

            string o = string.Empty;

            foreach (var s in dict)
            {
                string tmp = string.Format("{0}:{1} | ", s.Key, s.Value);

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
        /// <param name="adapter">Network adapter</param>
        /// <param name="pinfo">Packet information</param>
        /// <exception cref="ArgumentNullException"></exception>
        private Packet CreateLLDPPacket(NetworkInterface adapter, PacketInfo pinfo)
        {
            Debug.IndentLevel = 2;

            PhysicalAddress macAddress = adapter.GetPhysicalAddress();

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
                { "Ver", pinfo.OperatingSystemVersion }
            };

            // Port description
            Dictionary<string, string> portDescription = new Dictionary<string, string>();

            // Gateway
            if (ipProperties.GatewayAddresses.Count > 0)
            {
                portDescription.Add("GW", string.Join(", ", ipProperties.GatewayAddresses.Select(i => i.Address.ToString()).ToArray()).Trim().Trim(','));
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
                      w => w.IPv4Mask.AddressFamily == AddressFamily.InterNetwork
                    )
                    .Select(x => NetworkInfo.GetCIDRFromIPMaskAddress(x.IPv4Mask))
                    .ToArray()
                    ;

                portDescription.Add("NM", string.Join(", ", mask).Trim().Trim(','));
            }
            else
            {
                portDescription.Add("NM", "-");
            }


            // DNS server(s)
            if (ipProperties.DnsAddresses.Count > 0)
            {
                portDescription.Add("DNS", string.Join(", ", ipProperties.DnsAddresses.Select(i => i.ToString()).ToArray()).Trim().Trim(','));
            }
            else
            {
                portDescription.Add("DNS", "-");
            }

            // DHCP server
            if (ipProperties.DhcpServerAddresses.Count > 0)
            {
                portDescription.Add("DHCP", string.Join(", ", ipProperties.DhcpServerAddresses.Select(i => i.ToString()).ToArray()).Trim().Trim(','));
            }
            else
            {
                portDescription.Add("DHCP", "-");
            }

            // WINS server(s)
            if (ipProperties.WinsServersAddresses.Count > 0)
            {
                portDescription.Add("WINS", string.Join(", ", ipProperties.WinsServersAddresses.Select(i => i.ToString()).ToArray()).Trim().Trim(','));
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

            ushort expectedSystemCapabilitiesCapability = this.GetCapabilityOptionsBits(this.GetCapabilityOptions(adapter));
            ushort expectedSystemCapabilitiesEnabled = this.GetCapabilityOptionsBits(capabilitiesEnabled);

            // Constuct LLDP packet 
            LLDPPacket lldpPacket = new LLDPPacket();
            lldpPacket.TlvCollection.Add(new ChassisID(ChassisSubTypes.MACAddress, macAddress));
            lldpPacket.TlvCollection.Add(new PortID(PortSubTypes.LocallyAssigned, Encoding.UTF8.GetBytes(adapter.Name)));
            lldpPacket.TlvCollection.Add(new TimeToLive(120));
            lldpPacket.TlvCollection.Add(new PortDescription(this.CreateTlvString(portDescription)));

            lldpPacket.TlvCollection.Add(new SystemName(pinfo.MachineName));
            lldpPacket.TlvCollection.Add(new SystemDescription(this.CreateTlvString(systemDescription)));
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
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        lldpPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ip.Address), InterfaceNumbering.SystemPortNumber, Convert.ToUInt32(ipv4Properties.Index), string.Empty));
                    }
                }
            }

            // Add management IPv6 address(es)
            if (null != ipv6Properties)
            {
                foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        lldpPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ip.Address), InterfaceNumbering.SystemPortNumber, Convert.ToUInt32(ipv6Properties.Index), string.Empty));
                    }
                }
            }

            // End of LLDP packet
            lldpPacket.TlvCollection.Add(new EndOfLLDPDU());

            if (0 == lldpPacket.TlvCollection.Count)
            {
                throw new ArgumentException("Couldn't construct LLDP TLVs.");
            }

            if (lldpPacket.TlvCollection.Last().GetType() != typeof(EndOfLLDPDU))
            {
                throw new ArgumentException("Last TLV must be type of 'EndOfLLDPDU'!");
            }

            // Generate packet
            Packet packet = new EthernetPacket(macAddress, LldpDestinationMacAddress, EthernetPacketType.LLDP)
            {
                PayloadData = lldpPacket.Bytes
            };

            return packet;

        }

        /// <summary>
        /// Send packet using SharpPcap
        /// </summary>
        /// <param name="device">Network adapter device</param>
        /// <param name="payload">Packet to be sent</param>
        /// <returns>bool</returns>
        private bool SendRawPacket(PcapDevice device, Packet payload)
        {
            if (device == null)
            {
                return false;
            }

            if (device.Opened)
            {
                device.SendPacket(payload);
                device.Close();
                return true;
            }

            return false;
        }

        /// <summary>
        /// List possible LLDP capabilities such as:
        /// - Bridge 
        /// - Router 
        /// - WLAN Access Point 
        /// - Station 
        /// </summary>
        /// <param name="adapter">
        /// The adapter.
        /// </param>
        /// <returns>
        /// The <see cref="CapabilityOptions"/>.
        /// </returns>
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
        /// <param name="capabilities">
        /// The capabilities. <see cref="CapabilityOptions"/>
        /// </param>
        /// <returns>
        /// ushort
        /// </returns>
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