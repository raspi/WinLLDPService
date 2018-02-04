# See homepage for configuration details
$config = New-Object WinLLDPService.Configuration
#$config.PortDescription.Add("port descr..")
#$config.SystemDescription.Add("sys descr..")
#$config.SystemName = "my machine"

# Must always return WinLLDPService.Configuration object
Return $config