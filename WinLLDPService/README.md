# Developing WinLLDPService

Programmed in C#.

Use `Cli` subproject to quickly develop this project.

Uses:

* SharpPcap - https://www.nuget.org/packages/SharpPcap/
* Packet.NET - https://www.nuget.org/packages/PacketDotNet/

# How to build installer:
Uses WiX to build .msi installer package. See `build` directory for more information.

# How to debug
* Visual Studio's own debugging tools
* WireShark
* windump
* Switches' own possible LLDP debug logs
