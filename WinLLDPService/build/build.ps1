# Generate MSI installer
# Give architecture as parameter

param (
    [Parameter(Mandatory=$true)][string]$arch
)

Push-Location "$PSScriptRoot"

# Remove old files
Get-ChildItem -Path "$release_dir" -Include *.msi -File | foreach { $_.Delete()}

$release_dir = "$pwd/release"

if(![System.IO.Directory]::Exists("$release_dir")) {
	Write-Output "Creating '$release_dir'"
	Write-Output ""
	
	New-Item -ItemType Directory "$release_dir"
}

Write-Output "Removing old files from '$release_dir'"
Write-Output ""

# Remove old files
Get-ChildItem -Path "$release_dir" -Include *.* -File -Recurse | foreach { $_.Delete()}

$source_dir = "$pwd/../bin/$arch/Release/*"
$target_dir = "$release_dir"

Write-Output "Copying files from '$source_dir' -> '$target_dir'"
Write-Output ""

Copy-Item -Path "$source_dir" "$target_dir"

$output_installer_file = "$pwd/WinLLDPService-$arch.msi"

Write-Output "Building installer '$output_installer_file' .."
Write-Output ""

# Generate WiX version .wxi include file for .wxs 
Write-Output "Generating version file.."
$version_script = "$pwd/generate_version.ps1"

# Version include file
$stdErrLog = "$pwd/vergen-stderr.log"
$stdOutLog = "$pwd/vergen-stdout.log"
$process = Start-Process powershell -Wait -PassThru -NoNewWindow -ArgumentList "-File $version_script $release_dir/WinLLDPService.exe" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog

if ($process.ExitCode -ne 0) {
	Write-Output "ERROR: running version generator failed."
  	Get-Content $stdOutLog
	Get-Content $stdErrLog
	Exit 1
}

# Set environmental variables for WiX location
$paths_file = "$pwd/paths.ps1"

if([System.IO.File]::Exists("$paths_file")) {
  Write-Output ("Calling '{0}' to set environmental variables.." -f $paths_file)
  & $paths_file
} else {
  Write-Output ("ERROR: Paths file '{0}' not found. Rename or copy .example file to .ps1 file." -f $paths_file)
}

Write-Output ""

# Remove old installer file if it exists
if([System.IO.File]::Exists("$output_installer_file")) {
  Remove-Item "$output_installer_file"
}

Write-Output "Running WiX candle.."
$stdErrLog = "$pwd/candle-stderr.log"
$stdOutLog = "$pwd/candle-stdout.log"
$process = Start-Process candle -Wait -PassThru -NoNewWindow -ArgumentList "-v -ext WiXNetFxExtension -ext WixUtilExtension installer.wxs -arch $arch" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog
if ($process.ExitCode -ne 0) {
  Write-Output "ERROR: running WiX candle failed."
  
  Get-Content $stdOutLog
  Get-Content $stdErrLog  
  
  Exit 1
}

Write-Output ("-" * 40)
Write-Output ""

Write-Output "Running WiX light.."
$stdErrLog = "$pwd/light-stderr.log"
$stdOutLog = "$pwd/light-stdout.log"
$process = Start-Process light -Wait -PassThru -NoNewWindow -ArgumentList "-v -ext WixUIExtension -ext WiXNetFxExtension -ext WixUtilExtension -out $output_installer_file installer.wixobj" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog
if($process.ExitCode -ne 0) {
  Write-Output "ERROR: running WiX light failed."
  
  Get-Content $stdOutLog
  Get-Content $stdErrLog  
  
  Exit 1
}

Write-Output ("-" * 40)
Write-Output ""

# remove files created by WiX candle
Remove-Item "$pwd/*.wixobj"
Remove-Item "$pwd/*.wixpdb"

# Remove generated version include file
if([System.IO.File]::Exists("$version_file")) {
  Remove-Item "$version_file"
}


if(![System.IO.File]::Exists("$output_installer_file")) {
  Write-Output ("ERROR: .msi installer '{0}' not found." -f $output_installer_file)
  Exit 1
}

Write-Output "Build complete!"
Write-Output ""

Remove-Item "$pwd/*.log"

Pop-Location

Exit 0