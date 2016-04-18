using System;
using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using PacketDotNet.LLDP;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Collections.Generic;

namespace WinLLDPService
{
	class WinLLDP
	{
		public bool Run()
		{

            // Get list of network adapters			
			List<string> upLinks = GetUpAdaptersIds();
			
			if (upLinks.Count > 0) {
				// There are NIC(s) connected to network, send LLDP packet(s)
				SendLLDP(upLinks);
			}
			
			return true;
		}
		
		/// <summary>
		/// Fetch adapter IDs which state is UP (connected to network and is not loopback (127.0.0.1) device)
		/// </summary>
		/// <returns></returns>
		private List<string> GetUpAdaptersIds()
		{
			List<string> upLinks = new List<string>();
			
			NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
			
			foreach (NetworkInterface adapter in adapters) {
				if (adapter.OperationalStatus == OperationalStatus.Up && adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
					upLinks.Add(adapter.Id.ToLower());
				}
			}			
			
			return upLinks;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		private void SendLLDP(List<string> upLinks)
		{
			
			foreach (PcapDevice device in SharpPcap.CaptureDeviceList.Instance) {
				
				var devId = device.Name.ToLower();
				
				foreach (var up in upLinks) {
					if (devId.Contains(up)) {
						

						if (!device.Opened) {
							device.Open();
						}

						
						if (!device.Opened) {
							// Couldn't open network adapter, skip
							continue;
						}
						
						GetAdapterInfo(device);

						if (device.Opened) {
							device.Close();
						}
						
					}
				}
			}
			
		}
		
		/// <summary>
		/// Get adapter info and send LLDP packet if that adapter has IP(s)
		/// </summary>
		/// <returns></returns>
		private void GetAdapterInfo(PcapDevice device)
		{
			var ips = device.Interface.Addresses;
			
			foreach (var ip in ips) {
				
				if (ip.Addr.ipAddress != null) {
					// Send LLDP packet through specific adapter
					SendLLDPPacket(device, ip.Addr.ipAddress);
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
			
			foreach (var cap in capabilities) {
				ushort tmp = ushort.Parse(cap.GetHashCode().ToString());
				caps |= tmp;
			}
			
			return caps;
		}
				
				
		/// <summary>
		/// Send the actual LLDP packet to the switch
		/// </summary>
		/// <returns></returns>
		private void SendLLDPPacket(PcapDevice device, IPAddress ipAddress)
		{
			var managementAddressObjectIdentifier = "Management";
			string expectedPortDescription = device.Interface.FriendlyName;
			var capbits = GetCapabilityOptionsBits(GetCapabilityOptions());
			
			ushort expectedSystemCapabilitiesCapability = capbits;
			ushort expectedSystemCapabilitiesEnabled = capbits;

			var expectedOrganizationUniqueIdentifier = new byte[3] { 0x24, 0x10, 0x12 };
			var expectedOrganizationSpecificBytes = new byte[4] {
				0xBA,
				0xAD,
				0xF0,
				0x0D
			};

			uint managementAddressInterfaceNumber = 0x44060124;
			
			 
			// Constuct LLDP packet 
			var valuesLLDPPacket = new LLDPPacket();
			valuesLLDPPacket.TlvCollection.Add(new ChassisID(ChassisSubTypes.MACAddress, device.MacAddress));
			valuesLLDPPacket.TlvCollection.Add(new PortID(PortSubTypes.MACAddress, device.MacAddress));
			valuesLLDPPacket.TlvCollection.Add(new TimeToLive(120));			
			valuesLLDPPacket.TlvCollection.Add(new PortDescription(expectedPortDescription));
			valuesLLDPPacket.TlvCollection.Add(new SystemName(System.Environment.MachineName));
			valuesLLDPPacket.TlvCollection.Add(new SystemDescription(System.Environment.OSVersion.ToString()));
			valuesLLDPPacket.TlvCollection.Add(new SystemCapabilities(expectedSystemCapabilitiesCapability, expectedSystemCapabilitiesEnabled));
			valuesLLDPPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ipAddress), InterfaceNumbering.SystemPortNumber, managementAddressInterfaceNumber, managementAddressObjectIdentifier));
			
			// End of LLDP packet
			valuesLLDPPacket.TlvCollection.Add(new EndOfLLDPDU());
			
		
			byte[] destinationMAC = { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e };
			var destinationHW = new PhysicalAddress(destinationMAC);
		
			
			var packet = new EthernetPacket(device.MacAddress, destinationHW, EthernetPacketType.LLDP);
			packet.PayloadData = valuesLLDPPacket.Bytes;
		
			try {
				// Try to send the packet to network
				// If it fails, Windows Event Log will log the error(s)
				device.SendPacket(packet);
			} catch (DeviceNotReadyException e) {
				// Device was not ready (middle of initializing, cable just plugged in or something else)
				// Ignore and try again later
				return;
			}
			
		}
		
			
	}
}