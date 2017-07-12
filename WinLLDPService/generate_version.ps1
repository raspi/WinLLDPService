# Update AssemblyInfo files

Push-Location "$PSScriptRoot"

if(![System.IO.Directory]::Exists("$pwd/../.git")) {
	Write-Host "Git directory not found."
	Exit 0
}

# Get version from git
Write-Host "Running git to get latest date."
Write-Host ""

$stdErrLog = "$pwd/git-stderr.log"
$stdOutLog = "$pwd/git-stdout.log"
$process = Start-Process git -ArgumentList "log -1 --date=format:%y.%m.%d.%H%M --pretty=format:%cd" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog -Wait -PassThru -NoNewWindow

if ($process.ExitCode -ne 0) {
  Write-Host "ERROR: running git failed."
  
  Get-Content $stdOutLog
  Get-Content $stdErrLog  
  
  Exit 1
}

$ver = (Get-Content $stdOutLog)

if([System.IO.File]::Exists("$stdOutLog")) {
  Remove-Item "$stdOutLog"
}

if([System.IO.File]::Exists("$stdErrLog")) {
  Remove-Item "$stdErrLog"
}

Write-Host "Finding AssemblyInfo files.."
Write-Host ""

Get-ChildItem -Path "$pwd" -Filter "SharedAssemblyInfo.cs" -Recurse -Name | ForEach-Object {
	Write-Host "Reading file '$_'."
	$lines = (Get-Content "$_" -Encoding UTF8)
	$len = $lines.Count
	
	for ($i=0; $i -lt $len; $i++) {
		$line = $lines[$i]
		
		if ($line.StartsWith('[assembly: AssemblyVersion("')) {
			Write-Host "Replacing AssemblyVersion"
			$lines[$i] = '[assembly: AssemblyVersion("{0}")]' -f $ver
		}
		
		if ($line.StartsWith('[assembly: AssemblyFileVersion("')) {
			Write-Host "Replacing AssemblyFileVersion"
			$lines[$i] = '[assembly: AssemblyFileVersion("{0}")]' -f $ver
		}
		
	}

	Write-Host "Saving file '$_'."
	$lines | Set-Content "$_" -Encoding UTF8
	Write-Host ""
}

Write-Host "Done."

Pop-Location

Exit 0