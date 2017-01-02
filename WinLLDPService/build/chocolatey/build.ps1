param (
    [Parameter(Mandatory=$true)][string]$githubtag
)


$release_dir = "$pwd/release"

if([System.IO.Directory]::Exists("$release_dir")) {
	Write-Host "Removing '$release_dir'"
	Write-Host ""
	Remove-Item -Recurse "$release_dir"
}

if(![System.IO.Directory]::Exists("$release_dir")) {
	Write-Host "Creating '$release_dir'"
	Write-Host ""
	New-Item -ItemType Directory "$release_dir"
}

Copy-Item -Recurse "$pwd/template/*" "$release_dir"


$releaseParams = @{
   Uri = "https://api.github.com/repos/raspi/WinLLDPService/releases/tags/$githubtag";
   Method = 'GET';
   ContentType = 'application/json';
}

Write-Host "Getting release from github.."
$result = Invoke-RestMethod @releaseParams

$nuspecFile = "$release_dir/winlldpservice.nuspec"

Write-Host "Updating version to .nuspec file.."
$xml = [xml](Get-Content "$nuspecFile")
$xml.package.metadata.version = ("$githubtag" -Replace "v", "")
$xml.Save("$nuspecFile")

Write-Host "Done."
Write-Host ""

$installFile = "$release_dir/tools/chocolateyinstall.ps1"

Write-Host "Updating install file.."

$inst = (Get-Content "$installFile")

$tmp_file = "$release_dir/tmp.file"

if([System.IO.File]::Exists("$tmp_file")) {
	Remove-Item "$tmp_file"
}

foreach($a in $result.assets) {
	if ($a.name -like "*-x64.msi"){
		(New-Object System.Net.WebClient).DownloadFile($a.browser_download_url, "$tmp_file")
		$hash = (Get-FileHash -Algorithm SHA256 "$tmp_file").Hash
		
		if([System.IO.File]::Exists("$tmp_file")) {
			Remove-Item "$tmp_file"
		}
	
		$inst = $inst.replace("__URL64__", $a.browser_download_url)
		$inst = $inst.replace("__CHECKSUM64__", $hash)
	}
	
	if ($a.name -like "*-x86.msi"){
		(New-Object System.Net.WebClient).DownloadFile($a.browser_download_url, "$tmp_file")
		$hash = (Get-FileHash -Algorithm SHA256 "$tmp_file").Hash
		
		if([System.IO.File]::Exists("$tmp_file")) {
			Remove-Item "$tmp_file"
		}
		
		$inst = $inst.replace("__URL__", $a.browser_download_url)
		$inst = $inst.replace("__CHECKSUM__", $hash)
	}
	
}

$inst | Set-Content "$installFile"

Write-Host "Done."

Write-Host "Building chocolatey package.."

cd "$release_dir"
choco pack

Copy-Item "$release_dir/*.nupkg" "$release_dir/../"

cd "$release_dir/../"

if([System.IO.Directory]::Exists("$release_dir")) {
	Write-Host "Removing '$release_dir'"
	Write-Host ""
	Remove-Item -Recurse "$release_dir"
}
