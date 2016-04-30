# WinLLDPService
[![Build Status](https://travis-ci.org/raspi/WinLLDPService.svg?branch=master)](https://travis-ci.org/raspi/WinLLDPService)

Small LLDP Windows Service. 

Used by network switches to list network devices - https://en.wikipedia.org/wiki/Link_Layer_Discovery_Protocol

Sends LLDP packets to switch. Switch must have LLDP capability. It **doesn't** query switches' internal LLDP data. Errors are logged to Windows Event Log.

See homepage for more information and download `.msi` installer.

# Developing

See `WinLLDPService` directory.

# License
MIT

