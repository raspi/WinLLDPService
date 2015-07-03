/*
 * Created by SharpDevelop.
 * User: pekka
 * Date: 3.7.2015
 * Time: 6:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
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
			
			List<string> upLinks = GetUpAdaptersIds();
			
			if (upLinks.Count > 0) {
				SendLLDP(upLinks);
			}
			
			return true;
		}
		
		/// <summary>
		/// Fetch adapters which state is UP (connected to network)
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
		
		private void GetAdapterInfo(PcapDevice device)
		{
			var ips = device.Interface.Addresses;
			
			foreach (var ip in ips) {
				
				if (ip.Addr.ipAddress != null) {
					SendLLDPPacket(device, ip.Addr.ipAddress);
				}
						
			}
			
		}
		
		private List<CapabilityOptions> GetCapabilityOptions()
		{
			List<CapabilityOptions> capabilities = new List<CapabilityOptions>();
			capabilities.Add(CapabilityOptions.Bridge);
			capabilities.Add(CapabilityOptions.Router);
			capabilities.Add(CapabilityOptions.WLanAP);
			capabilities.Add(CapabilityOptions.StationOnly);
			
			return capabilities;
			
		}
				
		private ushort GetCapabilityOptionsBits(List<CapabilityOptions> capabilities)
		{
			ushort caps = 0;
			
			foreach (var cap in capabilities) {
				ushort tmp = ushort.Parse(cap.GetHashCode().ToString());
				caps |= tmp;
			}
			
			return caps;
			
		}
				
				
		
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
			
			 
			 
			var valuesLLDPPacket = new LLDPPacket();
			valuesLLDPPacket.TlvCollection.Add(new ChassisID(ChassisSubTypes.MACAddress, device.MacAddress));
			valuesLLDPPacket.TlvCollection.Add(new PortID(PortSubTypes.MACAddress, device.MacAddress));
			valuesLLDPPacket.TlvCollection.Add(new TimeToLive(120));			
			valuesLLDPPacket.TlvCollection.Add(new PortDescription(expectedPortDescription));
			valuesLLDPPacket.TlvCollection.Add(new SystemName(System.Environment.MachineName));
			valuesLLDPPacket.TlvCollection.Add(new SystemDescription(System.Environment.OSVersion.ToString()));
			valuesLLDPPacket.TlvCollection.Add(new SystemCapabilities(expectedSystemCapabilitiesCapability, expectedSystemCapabilitiesEnabled));
			valuesLLDPPacket.TlvCollection.Add(new ManagementAddress(new NetworkAddress(ipAddress), InterfaceNumbering.SystemPortNumber, managementAddressInterfaceNumber, managementAddressObjectIdentifier));
			
			// End
			valuesLLDPPacket.TlvCollection.Add(new EndOfLLDPDU());
			
		
			byte[] destinationMAC = { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e };
			var destinationHW = new PhysicalAddress(destinationMAC);
		
			
			var packet = new EthernetPacket(device.MacAddress, destinationHW, EthernetPacketType.LLDP);
			packet.PayloadData = valuesLLDPPacket.Bytes;
		
			try {
				device.SendPacket(packet);
			} catch (DeviceNotReadyException e) {
				return;
			}
			
		}
		
			
	}
}