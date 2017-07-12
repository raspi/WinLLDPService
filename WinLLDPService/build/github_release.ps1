# https://developer.github.com/v3/repos/releases/
# Give MSI file as a parameter
# Set environmental variable 'ghapikey' as your GitHub API key.

param (
    [Parameter(Mandatory=$true)][string]$msifile
)

Push-Location "$PSScriptRoot"

if (-not (Test-Path Env:ghapikey)) {
	Write-Output "Environmental variable 'ghapikey' is not set."
	Write-Output "Run:"
	Write-Output '$Env:ghapikey = "<your GitHub API KEY>"'
	Exit 1
}

$msifile = Resolve-Path "$msifile"

# Set environmental variables for WiX
# Set environmental variables for WiX location
$paths_file = "$pwd/paths.ps1"

if([System.IO.File]::Exists("$paths_file")) {
  Write-Output ("Calling '{0}' to set environmental variables.." -f $paths_file)
  & $paths_file
} else {
  Write-Output ("ERROR: Paths file '{0}' not found. Rename or copy .example file to .ps1 file." -f $paths_file)
}

# Register WiX MSI file information DLL
Add-Type -Path "$Env:WIXPATH\Microsoft.Deployment.WindowsInstaller.dll"

# Get version from MSI file
Write-Output "Getting version from .msi file.."
$db = new-object Microsoft.Deployment.WindowsInstaller.Database "$msifile"
$version = $db.ExecutePropertyQuery("ProductVersion")
$db.Close()

Write-Output "Version: $version"
Write-Output ""

$releaseParams = @{
   Uri = "https://api.github.com/repos/raspi/WinLLDPService/releases";
   Method = 'GET';
   Headers = @{
     Authorization = 'Basic ' + [Convert]::ToBase64String(
     [Text.Encoding]::ASCII.GetBytes($Env:ghapikey + ":x-oauth-basic"));
   }
   ContentType = 'application/json';
}

Write-Output "Getting old release on github.."
$result = Invoke-RestMethod @releaseParams

$id = 0
$old_version_found = $false

$result | ForEach-Object {
	if ($_.tag_name -eq "v$version") {
		$id = $_.id
		$old_version_found = $true
	}
}

if (!$old_version_found) {

	# Create new release to GitHub
	$releaseData = @{
	   tag_name = [string]::Format("v{0}", $version);
	   target_commitish = "master";
	   name = [string]::Format("v{0}", $version);
	   body = [string]::Format("v{0}", $version);
	   draft = $true;
	   prerelease = $false;
	}

	$releaseParams = @{
	   Uri = "https://api.github.com/repos/raspi/WinLLDPService/releases";
	   Method = 'POST';
	   Headers = @{
		 Authorization = 'Basic ' + [Convert]::ToBase64String(
		 [Text.Encoding]::ASCII.GetBytes($Env:ghapikey + ":x-oauth-basic"));
	   }
	   ContentType = 'application/json';
	   Body = (ConvertTo-Json $releaseData -Compress)
	}

	Write-Output "Making new release on github.."
	$result = Invoke-RestMethod @releaseParams
	$result

	$id = $result.id
}

# Upload file
$f = [System.IO.FileInfo] $msifile
$artifact = [string]::Format("{0}", $f.Name)

Write-Output $result

$uploadUri = $result | Where-Object {$_.tag_name -eq "v$version"} | Select -ExpandProperty upload_url

$uploadUri = [System.Uri]$uploadUri
$uploadUri = [string]::Format("{0}://{1}/repos/raspi/WinLLDPService/releases/{2}/assets?name={3}", $uploadUri.Scheme, $uploadUri.Host, $id, $artifact)

Write-Output "Upload URL: '$uploadUri'"

$uploadParams = @{
   Uri = $uploadUri;
   Method = 'POST';
   Headers = @{
     Authorization = 'Basic ' + [Convert]::ToBase64String(
     [Text.Encoding]::ASCII.GetBytes($Env:ghapikey + ":x-oauth-basic"));
   }
   ContentType = 'application/octet-stream';
   InFile = $msifile
}

Write-Output "Uploading msi file.."
$result = Invoke-RestMethod @uploadParams
$result

Write-Output ""
Write-Output "Now go to GitHub and release this version."
Write-Output ""

Exit 0