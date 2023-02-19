<#
.SYNOPSIS
Installs dependencies required to use the DebugTools Build Environment

.DESCRIPTION
The Install-DbgDependency installs dependencies required to utilize the DebugTools Build Environment. By default, Install-DbgDependency will install all dependencies that are required. A specific dependency can be installed by specifying a value to the -Name parameter. If dependencies are not installed, the DebugTools Build Environment will automatically install a given dependency for you when attempting to execute a command that requires it.

.PARAMETER Name
The dependencies to install. If no value is specified, all dependencies will be installed.

.EXAMPLE
C:\> Install-DbgDependency
Install all dependencies required to use the DebugTools Build Environment

.EXAMPLE
C:\> Install-DbgDependency Pester
Install the version of Pester required by the DebugTools Build Environment
#>
function Install-DbgDependency
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [ValidateSet(
            "chocolatey", "dotnet", "Pester", "Codecov", "OpenCover", "ReportGenerator",
            "VSWhere", "NuGet", "NuGetProvider", "PowerShellGet", "PSScriptAnalyzer",
            "net472"
        )]
        [string[]]$Name
    )

    Install-CIDependency $Name -Log:$false
}