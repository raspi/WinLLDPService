Push-Location "$PSScriptRoot"

$dll = "lldpbase.dll"
$paths = @("$pwd")

foreach($path in $paths) {
	$f = Join-Path -Path $path 
	if (Test-Path $f) {
		$dll = $f
		Return
	}
}

Add-Type -Path "$dll"

$config = New-Object WinLLDPService.Configuration
#$config.PortDescription.Add("port descr..")
#$config.SystemDescription.Add("sys descr..")

# Must return WinLLDPService.Configuration object
Return $config