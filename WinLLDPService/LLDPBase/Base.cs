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
        protected string ConfigurationFilePath { get; set; }

        /// <summary>
        /// The LLDP destination mac address.
        /// </summary>
        public static readonly PhysicalAddress LldpDestinationMacAddress = new PhysicalAddress(
            new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e }
        );

        public WinLLDP(string powerShellConfigurationScript)
        {
            this.ConfigurationFilePath = powerShellConfigurationScript;
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
            List<NetworkInterface> adapters = NetworkInterface.GetAllNetworkInterfaces()
                .Where(
                    x =>

                            // Link is up
                            x.OperationalStatus == OperationalStatus.Up

                            // Not loopback (127.0.0.1 / ::1)
                            && x.NetworkInterfaceType != NetworkInterfaceType.Loopback

                            // Not tunnel
                            && x.NetworkInterfaceType != NetworkInterfaceType.Tunnel

                            // Supports IPv4 or IPv6
                            && (x.Supports(NetworkInterfaceComponent.IPv4) || x.Supports(NetworkInterfaceComponent.IPv6)))
                .ToList();

            if (!adapters.Any())
            {
                // No available adapters
                return true;
            }

            // load configuration file 
            // see: Configuration.default.ps1
            Configuration config = PowerShellConfigurator.LoadConfiguration(this.ConfigurationFilePath).Result;

            Debug.WriteLine(config);

            CaptureDeviceList devices = CaptureDeviceList.Instance;

            // Wait time in milliseconds
            int waitTime = 10000;
            Stopwatch sw = new Stopwatch();

            foreach (NetworkInterface adapter in adapters)
            {
                sw.Reset();

                Debug.WriteLine(string.Format("Adapter '{0}':", adapter.Name), EventLogEntryType.Information);

                try
                {
                    Packet packet = this.CreateLLDPPacket(adapter, config);
                    Debug.IndentLevel = 0;

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
        /// Stop execution
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// Generate LLDP packet for adapter
        /// </summary>
        /// <param name="adapter">Network adapter</param>
        /// <param name="pinfo">Packet information</param>
        /// <exception cref="ArgumentNullException"></exception>
        private Packet CreateLLDPPacket(NetworkInterface adapter, Configuration config)
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

            config.PortDescription.Add(string.Format("{0}", adapter.Name));
            config.PortDescription.Add(string.Format("Spd: {0}", NetworkInfo.ReadableSize(adapter.Speed)));
            config.PortDescription.Add(string.Format("Vendor: {0}", adapter.Description));

            // Capabilities enabled
            List<CapabilityOptions> capabilitiesEnabled = new List<CapabilityOptions>
            {
                CapabilityOptions.StationOnly,
            };

            ushort systemCapabilities = this.GetCapabilityOptionsBits(this.GetCapabilityOptions(adapter));
            ushort systemCapabilitiesEnabled = this.GetCapabilityOptionsBits(capabilitiesEnabled);

            // Constuct LLDP packet 
            LLDPPacket lldpPacket = new LLDPPacket();

            if (config.ChassisType == ChassisType.MacAddress)
            {
                lldpPacket.TlvCollection.Add(new ChassisID(ChassisSubTypes.MACAddress, macAddress));
            }

            lldpPacket.TlvCollection.Add(new PortID(PortSubTypes.LocallyAssigned, Encoding.UTF8.GetBytes(adapter.Name)));
            lldpPacket.TlvCollection.Add(new TimeToLive(120));
            lldpPacket.TlvCollection.Add(new PortDescription(string.Join(config.Separator, config.PortDescription)));
            lldpPacket.TlvCollection.Add(new SystemName(string.Join(config.Separator, config.SystemName)));
            lldpPacket.TlvCollection.Add(new SystemDescription(string.Join(config.Separator, config.SystemDescription)));
            lldpPacket.TlvCollection.Add(new SystemCapabilities(systemCapabilities, systemCapabilitiesEnabled));

            // Add management address(es)
            if (ipv4Properties != null)
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
            if (ipv6Properties != null)
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

            if (lldpPacket.TlvCollection.Count == 0)
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