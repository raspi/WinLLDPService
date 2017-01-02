$ErrorActionPreference = 'Stop';

$packageName= 'winlldpservice'
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$url        = '__URL__'
$url64      = '__URL64__'

$packageArgs = @{
  packageName   = $packageName
  unzipLocation = $toolsDir
  fileType      = 'MSI'
  url           = $url
  url64bit      = $url64
  softwareName  = 'winlldpservice*'
  checksum      = '__CHECKSUM__'
  checksumType  = 'sha256'
  checksum64    = '__CHECKSUM64__'
  checksumType64= 'sha256'
  silentArgs    = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes= @(0, 3010, 1641)

}

Install-ChocolateyPackage @packageArgs
