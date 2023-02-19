<#
.SYNOPSIS
Analyzes best practice rules on DebugTools PowerShell files

.DESCRIPTION
The Invoke-DbgAnalyzer cmdlet analyzes best practice rules on the DebugTools repository on all PowerShell script files. By default, Invoke-DbgAnalyzer will report violations on all files in the DebugTools repository. This can be limited to a particular subset of files by specifying a wildcard to the -Name parameter.

For certain rule violations, Invoke-DbgAnalyzer can automatically apply the recommended fixes for you by specifying the -Fix parameter. To view the changes that will be applied it is recommended to also apply the -WhatIf parameter, and to have a clean Git working directory so that you may undo all of the changes or apply them in a single commit as desired.

.PARAMETER Name
A wildcard expression used to limit the files that should be analyzed.

.PARAMETER Fix
Automatically fix any rule violations where possible.

.EXAMPLE
C:\> Invoke-DbgAnalyzer
Report on violations across all PowerShell files in the DebugTools repository.

.EXAMPLE
C:\> Invoke-DbgAnalyzer *channnel*
Report on all violations across all PowerShell files whose name contains the word 'channel'.

.EXAMPLE
C:\> Invoke-DbgAnalyzer -Fix -WhatIf
View corrections that will be performed on rule violations.

.EXAMPLE
C:\> Invoke-DbgAnalyzer -Fix
Automatically fix all rule violations where possible.
#>
function Invoke-DbgAnalyzer
{
    [CmdletBinding(SupportsShouldProcess = $true)]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [switch]$Fix
    )

    if([string]::IsNullOrWhiteSpace($Name))
    {
        $Name = "*"
    }

    Install-CIDependency PSScriptAnalyzer

    Get-CallerPreference $PSCmdlet $ExecutionContext.SessionState

    $root = Get-SolutionRoot

    Write-DbgProgress "Invoke-DbgAnalyzer" "Identifying all script files..."

    $packages = Join-Path $root "packages"

    $files = gci $root -Filter $Name -Include *.ps1,*.psm1,*.psd1 -Recurse | where { $_.FullName -notlike "$packages*" }

    for($i = 0; $i -lt $files.Count; $i++)
    {
        Write-DbgProgress "Invoke-DbgAnalyzer" "Analyzing '$($files[$i].Name)' ($($i + 1)/$($files.Count))" -PercentComplete (($i + 1) / $files.Count * 100)

        $scriptArgs = @{
            Path = $files[$i].FullName
            IncludeRule = $script:rules
            WhatIf = $WhatIfPreference
            Fix = $Fix
        }

        Invoke-ScriptAnalyzer @scriptArgs
    }

    Complete-DbgProgress
}

$script:rules = @(
    "PSAvoidUsingCmdletAliases"
    "PSAvoidDefaultValueForMandatoryParameter"
    "PSAvoidGlobalAliases"
    "PSAvoidGlobalFunctions"
    "PSReservedParams"
    "PSPossibleIncorrectComparisonWithNull"
    "PSPossibleIncorrectUsageOfAssignmentOperator"
    "PSUseBOMForUnicodeEncodedFile"
    "PSUseConsistentIndentation"
    "PSUseConsistentWhitespace"
    "PSUseCorrectCasing"
    "PSUseLiteralInitializerForHashtable"
    "PSUseUTF8EncodingForHelpFile"
)