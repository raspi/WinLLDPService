# Generate version .wxi include file for .wxs
if($args.Length -ne 1){
  Write-Host "No parameters given."
  Exit 1
}

# Get version from git
Write-Host "Running git to get latest date."
$stdErrLog = "$pwd/git-stderr.log"
$stdOutLog = "$pwd/git-stdout.log"
$process = Start-Process git -ArgumentList "log -1 --pretty=format:%at" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog -Wait -PassThru -NoNewWindow
if($process.ExitCode -ne 0) {
  Write-Host "ERROR: running git failed."
  
  Get-Content $stdOutLog
  Get-Content $stdErrLog  
  
  Exit 1
}

$gitdate = [DateTime]::ParseExact("1/1/1970", "d/m/yyyy", [System.Globalization.CultureInfo]::InvariantCulture)

if ($gitdate.GetType() -ne [DateTime]){
  Write-Host "ERROR: ´$gitdate has wrong object type!"
  Exit 1
}

$gitdate = $gitdate.AddSeconds((Get-Content $stdOutLog))

if([System.IO.File]::Exists("$version_file_name")) {
  Remove-Item "$version_file_name"
}

Write-Host $gitdate

$version_file_name = $Args[0]

if([System.IO.File]::Exists("$stdOutLog")) {
  Remove-Item "$stdOutLog"
}

if([System.IO.File]::Exists("$stdErrLog")) {
  Remove-Item "$stdErrLog"
}


$version_file = New-Object System.IO.StreamWriter($version_file_name) 

$version_file.WriteLine("<?xml version='1.0' encoding='utf-8' ?>")
$version_file.WriteLine("<Include>")
$version_file.WriteLine("  <?define MajorVersion='{0}' ?>", ($gitdate.Year.ToString().Substring(2)))
$version_file.WriteLine("  <?define MinorVersion='{0}' ?>", $gitdate.Month.ToString()) 
$version_file.WriteLine("  <?define BuildVersion='{0}' ?>", $gitdate.Day.ToString()) 
$version_file.WriteLine("  <?define Revision='{0}' ?>", $gitdate.ToString("HHmm"))
$version_file.WriteLine('  <?define VersionNumber="$(var.MajorVersion).$(var.MinorVersion).$(var.BuildVersion).$(var.Revision)" ?>')
$version_file.WriteLine("  <?define UpgradeCode='{0}' ?>", "f97f19d5-ae5a-499c-b00b-1abcec53e393") 
$version_file.WriteLine("</Include>")

$version_file.Close()

if(![System.IO.File]::Exists($version_file_name)) {
  Write-Host ("ERROR: Version file '{0}' not found." -f $version_file_name)
  Exit 1
}
else
{
  Write-Host ("Version file '{0}' created." -f $version_file_name)
  Write-Host ("-" * 40)  
  Get-Content $version_file_name
  Write-Host ("-" * 40)  
  Write-Host ""
}

Exit 0