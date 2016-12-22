# WinLLDPService
[![Build Status](https://travis-ci.org/raspi/WinLLDPService.svg?branch=master)](https://travis-ci.org/raspi/WinLLDPService)
[![Build status](https://ci.appveyor.com/api/projects/status/7mhyl2fvasumvpqv?svg=true)](https://ci.appveyor.com/project/raspi/winlldpservice)
[![Github All Releases](https://img.shields.io/github/downloads/raspi/WinLLDPService/total.svg)]()

Small LLDP Windows Service. 

Used by network switches to list network devices - https://en.wikipedia.org/wiki/Link_Layer_Discovery_Protocol

Sends LLDP packets to switch. Switch must have LLDP capability. It **doesn't** query switches' internal LLDP data. Errors are logged to Windows Event Log.

See [homepage](https://raspi.github.io/projects/winlldpservice/) for more information and download `.msi` installer.

# Developing

See `WinLLDPService` directory.

# License
MIT

