Generate MSI installer from Release .exe. 

Rename `paths.ps1.example` to `paths.ps1` and set proper PATH environmental variables for WiX.

Build Release version with Visual Studio first.

WiX:
 
* http://wix.sourceforge.net/
* http://wixtoolset.org/

Run `build.ps1 <arch>` to generate `.msi` installer file.

Example: 

    build.ps1 x64

Make release to GitHub:

    github_release.ps1 WinLLDPService-x64.msi
    github_release.ps1 WinLLDPService-x86.msi
