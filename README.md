# WinLLDPService

[![Join the chat at https://gitter.im/raspi/WinLLDPService](https://badges.gitter.im/raspi/WinLLDPService.svg)](https://gitter.im/raspi/WinLLDPService?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Build Status](https://travis-ci.org/raspi/WinLLDPService.svg?branch=master)](https://travis-ci.org/raspi/WinLLDPService)
[![Build status](https://ci.appveyor.com/api/projects/status/7mhyl2fvasumvpqv?svg=true)](https://ci.appveyor.com/project/raspi/winlldpservice)
[![Github All Releases](https://img.shields.io/github/downloads/raspi/WinLLDPService/total.svg)]()

Small LLDP Windows Service. For *nix operating system(s) use the excellent [lldpd](https://github.com/vincentbernat/lldpd/).

Used by network switches to list network devices - https://en.wikipedia.org/wiki/Link_Layer_Discovery_Protocol

Sends LLDP packets to switch. Switch must have LLDP capability. It **doesn't** query switches' internal LLDP data. Errors are logged to Windows Event Log.

See [homepage](https://raspi.github.io/projects/winlldpservice/) for more information and download `.msi` installer.

# Developing

See `WinLLDPService` directory.

# License
MIT

# Thanks to

* [JetBrains](https://www.jetbrains.com/) for [ReSharper](https://www.jetbrains.com/resharper/) Open Source license
