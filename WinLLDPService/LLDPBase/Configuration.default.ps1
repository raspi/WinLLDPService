# See homepage for configuration details
$config = New-Object WinLLDPService.Configuration
#$config.PortDescription.Add("port descr..")
#$config.SystemDescription.Add("sys descr..")
#$config.SystemName.Clear()
#$config.SystemName.Add("my machine")
#$config.Separator = "|"

# Must always return WinLLDPService.Configuration object
Return $config