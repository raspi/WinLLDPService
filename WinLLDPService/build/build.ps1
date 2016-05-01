$output_installer_file = "$pwd/WinLLDPService-installer.msi"

Write-Host "Building installer.."
Write-Host ""

# Generate WiX version .wxi include file for .wxs 
Write-Host "Generating version file.."
$version_script = "$pwd/generate_version.ps1"

# Version include file
$version_file = "$pwd/version.wxi"
$process = Start-Process powershell -Wait -PassThru -NoNewWindow -ArgumentList "-File $version_script $version_file"

if(![System.IO.File]::Exists($version_file)) {
  Write-Host ("ERROR: Version file '{0}' not found." -f $version_file)
  Exit 1
}

# Set environmental variables for WiX
Write-Host ("Calling '{0}'.." -f $paths_file)
$paths_file = "$pwd/paths.ps1"
if(![System.IO.File]::Exists($paths_file)) {
  Write-Host ("ERROR: Paths file '{0}' not found." -f $paths_file)
  exit 1
}

& $paths_file

# Remove old installer file if it exists
if([System.IO.File]::Exists($output_installer_file)) {
  Remove-Item $output_installer_file
}

Write-Host "Running WiX candle.."
$process = Start-Process candle -Wait -PassThru -NoNewWindow -ArgumentList '-v -ext WiXNetFxExtension -ext WixUtilExtension installer.wxs'
if($process.ExitCode -ne 0) {
  Write-Host "ERROR: running WiX candle failed."
  Write-Host ($process.StandardOutput.ReadToEnd())
  Write-Host ($process.StandardError.ReadToEnd())
  Exit 1
}

Write-Host ("-" * 40)
Write-Host ""

Write-Host "Running WiX light.."
$process = Start-Process light -Wait -PassThru -NoNewWindow -ArgumentList "-v -ext WixUIExtension -ext WiXNetFxExtension -out $output_installer_file installer.wixobj"
if($process.ExitCode -ne 0) {
  Write-Host "ERROR: running WiX light failed."
  Write-Host ($process.StandardOutput.ReadToEnd())
  Write-Host ($process.StandardError.ReadToEnd())
  Exit 1
}

# Remove generated version include file
if([System.IO.File]::Exists($version_file)) {
  Remove-Item $version_file
}

Write-Host ("-" * 40)
Write-Host ""

# remove files created by WiX candle
Remove-Item "$pwd/*.wixobj"
Remove-Item "$pwd/*.wixpdb"

if(![System.IO.File]::Exists($output_installer_file)) {
  Write-Host ("ERROR: .msi installer '{0}' not found." -f $output_installer_file)
  exit 1
}

Write-Host "Build complete!"
Write-Host ""

Write-Host "Press any key to continue..."
$null = [System.Console]::ReadKey()