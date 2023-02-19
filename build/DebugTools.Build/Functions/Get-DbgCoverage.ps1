<#
.SYNOPSIS
Generates a code coverage report for DebugTools

.DESCRIPTION
The Get-DbgCoverage cmdlet generates a code coverage report for DebugTools. By default, all tests will be executed using the Debug configuration. Coverage analysis can be limited to a subset of tests by specifying a wildcard to the -Name parameter.

When the coverage analysis has completed, a HTML report detailing the results of the analysis will automatically be opened in your default web browser.

.PARAMETER Name
Wildcard used to limit coverage to those whose test names match a specified pattern.

.PARAMETER Type
Types of tests to generate coverage for. If no type is specified, both C# and PowerShell test coverage will be generated.

.PARAMETER Configuration
Build configuration to use when calculating coverage. If no configuration is specified, Debug will be used.

.PARAMETER TestOnly
Run the test commands used by OpenCover without collecting coverage.

.PARAMETER SkipReport
Skip generating a HTML report upon generating code coverage.

.EXAMPLE
C:\> Get-DbgCoverage
Generate a code coverage report

.EXAMPLE
C:\> Get-DbgCoverage *dynamic*
Generate a code coverage report of all tests whose name contains the word "dynamic"

.LINK
Invoke-DbgBuild
#>
function Get-DbgCoverage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Name = "*",

        [Parameter(Mandatory = $false)]
        [ValidateSet('C#', 'PowerShell')]
        [string[]]$Type,

        [Parameter(Mandatory=$false)]
        [Configuration]$Configuration = "Debug",

        [Parameter(Mandatory = $false)]
        [switch]$TestOnly,

        [Parameter(Mandatory = $false)]
        [switch]$SkipReport
    )

    Write-Warning "Coverage is not supported"

    return

    $coverageArgs = @{
        Name = $Name
        BuildFolder = (Get-SolutionRoot)
        Type = $Type
        Configuration = $Configuration
        TestOnly = $TestOnly
    }

    Get-CodeCoverage @coverageArgs

    if($TestOnly -or $SkipReport)
    {
        return
    }

    $date = (Get-Date).ToString("yyyy-MM-dd_HH-mm-ss")

    $dir = Join-Path ([IO.Path]::GetTempPath()) "DbgCoverage_$($date)"

    Write-Host -ForegroundColor Cyan "Generating coverage report in $dir"

    New-CoverageReport -TargetDir $dir

    Start-Process (Join-Path $dir "index.htm")
}