### Submission type
<!-- Remove these comment lines and fill out the "> …" lines and possible checkboxes with [x] -->

  - [ ] Bug report
  - [ ] Request for enhancement

<!-- NOTE: Do not submit anything other than bug reports or enhancements via the issue tracker! -->
<!-- NOTE: Only submit one enhancement or bug per issue! -->

### Used version of operating system
<!-- NOTE: For example: Windows 7 Ultimate 64 bit -->

> …

### In case of bug report: Is machine connected to Active Directory?

<!-- NOTE: Specify what Windows version is running AD -->
<!-- NOTE: Specify if you're using Samba ( https://www.samba.org/ ) as AD -->

 - [ ] Yes

> …

### In case of bug report: Version the issue has been seen with

> …

<!-- NOTE: Do not submit bug reports about anything but the two most recently released versions! -->
<!-- NOTE: see homepage https://raspi.github.io/projects/winlldpservice/ for latest releases -->

### In case of bug report: Capture library used
<!-- For example: npcap v0.97 -->
<!-- Did you try also with different capture library (npcap, win10pcap, winpcap, ..)? -->

> …

<!-- If npcap was used add also the logs: https://github.com/nmap/npcap#bug-report (Diagnostic report, General installation log, Driver installation log) -->
<!-- If winpcap was used add also the logs: https://winpcap.org/bugs.htm -->
<!-- If win10pcap was used add also the logs: see https://winpcap.org/bugs.htm -->

### In case of bug report: PowerShell information

PowerShell version used
<!-- Run: $PSVersionTable.PSVersion | Format-List -->

> …

What are your execution policies?
<!-- Run: Get-ExecutionPolicy -List -->

> …

### In case of bug report: Expected behaviour you didn't see
<!-- NOTE: For example: switch didn't receive any LLDP information -->
<!-- NOTE: Attach possible log(s) and screenshot(s). -->

> …

### In case of bug report: Unexpected behaviour you saw
<!-- NOTE: For example: service tried to send malformed packet -->

> …

### In case of bug report: Steps to reproduce the problem

> …

### In case of bug report: Additional details
<!-- Any errors on Windows Event Log? -->
<!-- Switch(es) are running latest firmware and LLDP support is enabled? -->
<!-- Other than ASCII characters in network interface or computer name? -->
<!-- Which .NET framework version are you using? -->
<!-- MSI installer version if applicable (run "msiexec /?") (for example "Windows® Installer. V5.0.7601.23593") -->
<!-- Did you try to update msiexec and/or .NET framework? -->
<!-- Did you try also the 32 bit installer on 64 bit system? -->
<!-- Did you try on different machine(s)? -->
<!-- If installer failed to install the software please provide installer `.log` as attachment by running (as admin): -->
<!--
    msiexec /i "installer.msi" /L*V "install.txt" 
-->
<!-- List of adapters (PowerShell): -->
<!-- 
  Get-NetAdapter | select *
-->
<!-- List of adapters (PowerShell): -->
<!-- 
  Get-WmiObject -Class Win32_NetworkAdapter | select *
-->

> …
