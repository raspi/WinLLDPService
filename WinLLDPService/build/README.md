# Requirements

* OS: Windows 2012 R2 or Windows 8.1 or better 
* Support for .NET framework `4.5.1` (2013) or better
* Visual Studio 2017 Community Edition or better
* PowerShell
* WiX: http://wixtoolset.org/ http://wix.sourceforge.net/ 

# Configure build scripts

Rename `paths.ps1.example` to `paths.ps1` (once) and set proper PATH environmental variables for WiX path. "Usually  `C:\Program Files (x86)\WiX Toolset <version>\`". `bin` path contains `candle`, `light` and DLLs required for getting information about generated `.msi` files.

# Building release

Build Release version with Visual Studio first (batch build).

Run `build.ps1 <arch>` to generate `.msi` installer file.

Example: 

    build.ps1 x64

This will generate `WinLLDPService-x64.msi` file to `build` directory.

Install and test the installer:

 * Check Windows Event log for possible errors 
 * Switch sees LLDP information
 * Additionally check with network debuggin tool such as WireShark or Microsoft Message Analyzer.
 * Uninstall and test also other version (`x64`,` x86`)

If there's installing problems use `WinLLDPService-installer.msi /l*vx install.log` to create install log. Refer to [WiX manual](http://wixtoolset.org/documentation/manual/) and [Microsoft Installer documentation](https://msdn.microsoft.com/en-us/library/windows/desktop/cc185688(v=vs.85).aspx). 

# Make release to GitHub:

Requires that you've generated `.msi` installers first.

Start PowerShell and go to `build` directory.

    $Env:ghapikey="<your GitHub API key>"
    github_release.ps1 WinLLDPService-x64.msi
    github_release.ps1 WinLLDPService-x86.msi

Now go to https://github.com/raspi/WinLLDPService/releases select draft and **Publish** the new version.

# Make release to Chocolatey:

See [Chocolatey](chocolatey/) directory for documentation.