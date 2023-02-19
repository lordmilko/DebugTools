<#
.SYNOPSIS
Creates NuGet packages from DebugTools for distribution

.DESCRIPTION
The New-DbgPackage generates NuGet packages from DebugTools for distribution within a NuGet package management system. By default, packages will be built using the last Debug build for both the C# and PowerShell versions of DebugTools. Packages can be built for a specific project type by specifying a value to the -Type parameter. Upon generating a package, a FileInfo object will be emitted to the pipeline indicating the name and path to the generated package.

Unlike packaging done in CI builds, New-DbgPackage does not verify that the contents of the generated package are correct.

.PARAMETER Type
Type of packages to create. By default C# and PowerShell packages as well as a redistributable zip file are created.

.PARAMETER Configuration
Configuration to pack. If no value is specified, the last Debug build will be packed.

.EXAMPLE
C:\> New-DbgPackage
Create NuGet packages for both C# and PowerShell

.EXAMPLE
C:\> New-DbgPackage -Type PowerShell
Create NuGet packages for PowerShell only

.EXAMPLE
C:\> New-DbgPackage -Configuration Release
Create Release NuGet packages for both C# and PowerShell

.LINK
Invoke-DbgBuild
#>
function New-DbgPackage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [ValidateSet('PowerShell', 'Redist')]
        [string[]]$Type,

        [Parameter(Mandatory=$false)]
        [Configuration]$Configuration = "Debug"
    )

    $root = Get-SolutionRoot

    $manager = New-PackageManager

    $text = "PowerShell"

    Write-DbgProgress "New-DbgPackage" "Creating $text Package" -PercentComplete 50
        
    $manager.InstallPowerShellRepository()

    $binDir = Get-PowerShellOutputDir $root $Configuration

    $powershellArgs = @{
        OutputDir = $binDir
        RepoManager = $manager
        Configuration = $Configuration
        Redist = $Type | HasType "Redist"
        PowerShell = $Type | HasType "PowerShell"
    }

    New-PowerShellPackage @powershellArgs

    Move-Packages "_PowerShell" $root

    # Don't uninstall the repository unless we succeeded, so we can troubleshoot any issues
    # inside the repository incase the pack fails
    $manager.UninstallPowerShellRepository()

    Complete-DbgProgress
}