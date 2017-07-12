# Generate version .wxi include file for .wxs

param (
    [Parameter(Mandatory=$true)][string]$exe_file_name
)

Push-Location "$PSScriptRoot"

$version_file_name="$pwd/version.wxi"

if(![System.IO.File]::Exists("$exe_file_name")) {
	Write-Output ("File not found: '{0}'!" -f "$exe_file_name")
	Exit 1
}

# Get version from .exe
Write-Output ("Getting version from file '{0}'" -f "$exe_file_name")
$ver = ((Get-Item "$exe_file_name").VersionInfo).ProductVersion.ToString() -Split "\."
Write-Output ("Version: '{0}'" -f "$ver")

if([System.IO.File]::Exists("$version_file_name")) {
  # Remove old file
  Remove-Item "$version_file_name"
}

$version_file = New-Object System.IO.StreamWriter("$version_file_name") 

$version_file.WriteLine("<?xml version='1.0' encoding='utf-8' ?>")
$version_file.WriteLine("<Include>")
$version_file.WriteLine("  <?define MajorVersion='{0}' ?>", $ver[0])
$version_file.WriteLine("  <?define MinorVersion='{0}' ?>", $ver[1]) 
$version_file.WriteLine("  <?define BuildVersion='{0}' ?>", $ver[2]) 
$version_file.WriteLine("  <?define Revision='{0}' ?>", $ver[3])
$version_file.WriteLine('  <?define VersionNumber="$(var.MajorVersion).$(var.MinorVersion).$(var.BuildVersion).$(var.Revision)" ?>')
$version_file.WriteLine("  <?define UpgradeCode='{0}' ?>", "f97f19d5-ae5a-499c-b00b-1abcec53e393") 
$version_file.WriteLine("</Include>")

$version_file.Close()

if(![System.IO.File]::Exists("$version_file_name")) {
  Write-Output ("ERROR: Version file '{0}' not found." -f $version_file_name)
  Exit 1
}
else
{
  Write-Output ("Version file '{0}' created." -f $version_file_name)
  Write-Output ("-" * 40)  
  Get-Content $version_file_name
  Write-Output ("-" * 40)  
  Write-Output ""
}

Exit 0