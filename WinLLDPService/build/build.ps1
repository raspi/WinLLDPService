# Generate MSI installer
# Give architecture as parameter

param (
    [Parameter(Mandatory=$true)][string]$arch
)

$release_dir = "$pwd/release"

if(![System.IO.Directory]::Exists("$release_dir")) {
	Write-Host "Creating '$release_dir'"
	Write-Host ""
	
	New-Item -ItemType Directory "$release_dir"
}

Write-Host "Removing old files from '$release_dir'"
Write-Host ""

Get-ChildItem -Path "$release_dir" -Include *.* -File -Recurse | foreach { $_.Delete()}

$source_dir = "$pwd/../bin/$arch/Release/*"
$target_dir = "$release_dir"

Write-Host "Copying files from '$source_dir' -> '$target_dir'"
Write-Host ""

Copy-Item -Path "$source_dir" "$target_dir"

$output_installer_file = "$pwd/WinLLDPService-$arch.msi"

Write-Host "Building installer '$output_installer_file' .."
Write-Host ""

# Generate WiX version .wxi include file for .wxs 
Write-Host "Generating version file.."
$version_script = "$pwd/generate_version.ps1"

# Version include file
$version_file = "$pwd/version.wxi"
$stdErrLog = "$pwd/vergen-stderr.log"
$stdOutLog = "$pwd/vergen-stdout.log"
$process = Start-Process powershell -Wait -PassThru -NoNewWindow -ArgumentList "-File $version_script $version_file" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog

if ($process.ExitCode -ne 0) {
	Write-Host "ERROR: running version generator failed."
  	Get-Content $stdOutLog
	Get-Content $stdErrLog  
	Exit 1
}

if(![System.IO.File]::Exists("$version_file")) {
  Write-Host ("ERROR: Version file '{0}' not found." -f $version_file)
  Exit 1
}

# Set environmental variables for WiX
$paths_file = "$pwd/paths.ps1"
Write-Host ("Calling '{0}' to set environmental variables.." -f $paths_file)

if(![System.IO.File]::Exists("$paths_file")) {
  Write-Host ("ERROR: Paths file '{0}' not found. Rename or copy .example file to .ps1 file." -f $paths_file)
  $null = [System.Console]::ReadKey()
  Exit 1
}

& $paths_file

Write-Host ""

# Remove old installer file if it exists
if([System.IO.File]::Exists("$output_installer_file")) {
  Remove-Item "$output_installer_file"
}

Write-Host "Running WiX candle.."
$stdErrLog = "$pwd/candle-stderr.log"
$stdOutLog = "$pwd/candle-stdout.log"
$process = Start-Process candle -Wait -PassThru -NoNewWindow -ArgumentList "-v -ext WiXNetFxExtension -ext WixUtilExtension installer.wxs -arch $arch" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog
if ($process.ExitCode -ne 0) {
  Write-Host "ERROR: running WiX candle failed."
  
  Get-Content $stdOutLog
  Get-Content $stdErrLog  
  
  $null = [System.Console]::ReadKey()
  
  Exit 1
}

Write-Host ("-" * 40)
Write-Host ""

Write-Host "Running WiX light.."
$stdErrLog = "$pwd/light-stderr.log"
$stdOutLog = "$pwd/light-stdout.log"
$process = Start-Process light -Wait -PassThru -NoNewWindow -ArgumentList "-v -ext WixUIExtension -ext WiXNetFxExtension -ext WixUtilExtension -out $output_installer_file installer.wixobj" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog
if($process.ExitCode -ne 0) {
  Write-Host "ERROR: running WiX light failed."
  
  Get-Content $stdOutLog
  Get-Content $stdErrLog  
  
  Write-Host "Press any key to continue..."  
  $null = [System.Console]::ReadKey()
  
  Exit 1
}

Write-Host ("-" * 40)
Write-Host ""

# remove files created by WiX candle
Remove-Item "$pwd/*.wixobj"
Remove-Item "$pwd/*.wixpdb"

# Remove generated version include file
if([System.IO.File]::Exists("$version_file")) {
  Remove-Item "$version_file"
}


if(![System.IO.File]::Exists("$output_installer_file")) {
  Write-Host ("ERROR: .msi installer '{0}' not found." -f $output_installer_file)
  $null = [System.Console]::ReadKey()
  exit 1
}

Write-Host "Build complete!"
Write-Host ""

Remove-Item "$pwd/*.log"

Write-Host "Press any key to continue..."
$null = [System.Console]::ReadKey()