<#
.SYNOPSIS
Retrieves version information used by various components of DebugTools

.DESCRIPTION
The Get-DbgVersion cmdlet retrieves version details found in various locations in the DebugTools project. Version details can be updated using the Set-DbgVersion and Update-DbgVersion cmdlet. The following table details the version details that can be retrieved:

    | Property    | Source                                | Description                                |
    | ------------| ------------------------------------- | ------------------------------------------ |
    | Package     | build\Version.props                   | Version used when creating nupkg files     |
    | Assembly    | build\Version.props                   | Assembly Version used with assemblies      |
    | File        | build\Version.props                   | Assembly File Version used with assemblies |
    | Module      | DebugTools.PowerShell\DebugTools.psd1 | DebugTools PowerShell Module version       |
    | ModuleTag   | DebugTools.PowerShell\DebugTools.psd1 | DebugTools PowerShell Module Release Tag   |
    | PreviousTag | Git                                   | Version of previous GitHub Release         |

Note that if DebugTools detects that the .git folder is missing from the repo or that the "git" command is not installed on your system, the PreviousTag property will be omitted from results.

.EXAMPLE
C:\> Get-DbgVersion
Retrieve version information about the DebugTools project.

.LINK
Set-DbgVersion
Update-DbgVersion
#>
function Get-DbgVersion
{
    [CmdletBinding()]
    param()

    $root = Get-SolutionRoot

    Get-CallerPreference $PSCmdlet $ExecutionContext.SessionState -DefaultErrorAction "Continue"

    Get-CIVersion $root -ErrorAction $ErrorActionPreference
}