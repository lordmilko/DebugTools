<#
.SYNOPSIS
Retrieves commands that are available in the DebugTools Build Environment.

.DESCRIPTION
The Get-DbgCommand retrieves all commands that are available for use within the DebugTools Build Environment. Each command contains a description outlining what exactly that command does. The results from Get-DbgCommand can be filtered by specifying a wildcard expression matching part of the command's name you wish to find.

.PARAMETER Name
Wildcard used to filter results to a specific command.

.EXAMPLE
C:\> Get-DbgCommand
List all commands supported by the DebugTools Build Module

.EXAMPLE
C:\> Get-DbgCommand *build*
List all commands whose name contains "build"
#>
function Get-DbgCommand
{
    [CmdletBinding()]
    param(
        [ValidateScript( { 
            if([String]::IsNullOrWhiteSpace($_))
            {
                throw "The argument is null, empty or whitespace. Provide an argument that is not null, empty or whitespace and then try the command again."
            }

            return $true
        } )]
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Name = "*"
    )

    $excluded = @(
        "Test-DbgCI"
        "Write-DbgProgress"
        "Complete-DbgProgress"
    )

    if($script:getDbgCommandCache)
    {
        $script:getDbgCommandCache | where Name -Like $Name
    }
    else
    {
        $commands = gcm -Module DebugTools.Build -Name $Name | where { ($_.Name -Like "*-Dbg*" -or $_.Name -Like "*-Debug*") -and $_.Name -notin $excluded }

        $sorted = $commands | foreach {
            [PSCustomObject]@{
                Name = $_.Name
                Category = GetCategory $_.Name
                Description = (Get-Help $_.Name).Synopsis.Trim()
            }
        } | Sort-Object Category,Name

        if($Name -eq "*")
        {
            $script:getDbgCommandCache = $sorted
        }

        return $sorted
    }
}

function GetCategory($name)
{
    if($name -like "*-DbgVersion")
    {
        return "Version"
    }
    elseif($name -like "*-DbgBuild")
    {
        return "Build"
    }

    if($name -like "*-DbgTest*")
    {
        return "Test"
    }

    $ci = @(
        "Get-DbgCoverage"
        "New-DbgPackage"
        "Simulate-DbgCI"
    )

    if($name -in $ci)
    {
        return "CI"
    }

    $help = @(
        "Get-DbgCommand"
        "Get-DbgHelp"
    )

    if($name -in $help)
    {
        return "Help"
    }

    $utilities = @(
        "Get-DbgLog"
        "Install-DbgDependency"
        "Invoke-DbgAnalyzer"
        "Start-DebugTools"
    )

    if($name -in $utilities)
    {
        return "Utility"
    }

    throw "Don't know how to categorize cmdlet '$name'"
}