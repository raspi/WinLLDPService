Generate MSI installer from Release .exe. 

Rename `paths.ps1.example` to `paths.ps1` and set proper PATH environmental variables for WiX.

Build Release version with Visual Studio first.

WiX:
 
* http://wix.sourceforge.net/
* http://wixtoolset.org/

Run `build.ps1 `to generate `.msi` installer file.