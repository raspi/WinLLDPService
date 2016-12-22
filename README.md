# WinLLDPService
[![Build Status](https://travis-ci.org/raspi/WinLLDPService.svg?branch=master)](https://travis-ci.org/raspi/WinLLDPService)
[![Build status](https://ci.appveyor.com/api/projects/status/7mhyl2fvasumvpqv?svg=true)](https://ci.appveyor.com/project/raspi/winlldpservice)

Small LLDP Windows Service. 

Used by network switches to list network devices - https://en.wikipedia.org/wiki/Link_Layer_Discovery_Protocol

Sends LLDP packets to switch. Switch must have LLDP capability. It **doesn't** query switches' internal LLDP data. Errors are logged to Windows Event Log.

See homepage for more information and download `.msi` installer.

# Developing

See `WinLLDPService` directory.

# License
MIT

