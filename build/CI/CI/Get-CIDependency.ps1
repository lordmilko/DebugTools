function Get-CIDependency
{
    # If you add a new entry here also make sure to add it to Simulate-DbgCI.Tests.ps1, Install-DbgDependency.ps1 and
    # Install-DbgDependency.Tests.ps1 (including both the standalone test and the test as part of all dependencies)
    $dependencies = @(
        @{ Name = "chocolatey";               Chocolatey = $true;      MinimumVersion = "0.10.5.0";  Manager = $true }
        @{ Name = "dotnet";                   Dotnet     = $true }
        @{ Name = "codecov";                  Chocolatey = $true }
        @{ Name = "opencover.portable";       Chocolatey = $true;      MinimumVersion = "4.7.922.0"; CommandName = "opencover.console" }
        @{ Name = "reportgenerator.portable"; Chocolatey = $true;      MinimumVersion = "3.0.0.0";   CommandName = "reportgenerator" }
        @{ Name = "vswhere";                  Chocolatey = $true;      MinimumVersion = "2.6.7" }
        @{ Name = "NuGet.CommandLine";        Chocolatey = $true;      MinimumVersion = "5.2.0";     CommandName = "nuget" }
        @{ Name = "NuGetProvider";            PackageProvider = $true; MinimumVersion = "2.8.5.201" }
        @{ Name = "PowerShellGet";            PowerShell = $true;      MinimumVersion = "2.0.0" }
        @{ Name = "Pester";                   PowerShell = $true;      MinimumVersion = "3.4.5";     Version = "3.4.6"; SkipPublisherCheck = $true }
        @{ Name = "PSScriptAnalyzer";         PowerShell = $true }
        @{ Name = "net472";                   TargetingPack = $true;   Version = "4.7.2" }
    )

    return $dependencies
}
