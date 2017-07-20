# Developing WinLLDPService

Programmed in C#.

Use `Cli` subproject to quickly develop this project.

Uses:

* [SharpPcap](https://github.com/chmorgan/sharppcap) - https://www.nuget.org/packages/SharpPcap/
* [Packet.NET](https://github.com/chmorgan/packetnet) - https://www.nuget.org/packages/PacketDotNet/

# How to build installer:
Uses [WiX](http://wixtoolset.org/) to build `.msi` installer package. See `build` directory for more information.

# How to debug
* Visual Studio's own debugging tools
* [Microsoft Message Analyzer](https://www.microsoft.com/en-us/download/details.aspx?id=44226)
* [WireShark](https://www.wireshark.org/)
* windump
* Switches' own possible LLDP debug logs (SSH, telnet, web UI, ..)

# Resources
* http://standards.ieee.org/getieee802/download/802.1AB-2009.pdf
* https://wiki.wireshark.org/LinkLayerDiscoveryProtocol
* https://github.com/chmorgan/packetnet/blob/master/Test/PacketType/LldpTest.cs
* https://en.wikipedia.org/wiki/Link_Layer_Discovery_Protocol
