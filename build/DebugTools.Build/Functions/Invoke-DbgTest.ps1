<#
.SYNOPSIS
Executes tests on a DebugTools build.

.DESCRIPTION
The Invoke-DbgTest cmdlet executes tests on previously generated builds of DebugTools. By default both C# and PowerShell tests will be executed against the last Debug build. Tests can be limited to a specific platform by specifying a value to the -Type parameter, and can also be limited to those whose name matches a specified wildcard expression via the -Name parameter.

Tests executed by Invoke-DbgTest are automatically logged in the TRX format (C#) and NUnitXml format (PowerShell) under the Profiler.Tests\TestResults folder of the DebugTools solution. Test results in this directory can be evaluated and filtered after the fact using the Get-DbgTestResult cmdlet. Note that upon compiling a new build of Profiler.Tests, all items in this test results folder will automatically be deleted.

.PARAMETER Name
Wildcard used to specify tests to execute. If no value is specified, all tests will be executed.

.PARAMETER Type
Type of tests to execute. If no value is specified, both C# and PowerShell tests will be executed.

.PARAMETER Configuration
Build configuration to test. If no value is specified, the last Debug build will be tested.

.PARAMETER Tag
Specifies tags or test categories to execute. If a Name is specified as well, these two categories will be filtered using logical AND.

.PARAMETER Build
Specifies that DebugTools.Build tests should be included. If -Tag contains "Build" this parameter will also be activated.

.EXAMPLE
C:\> Invoke-DbgTest
Executes all unit tests on the last DebugTools build.

.EXAMPLE
C:\> Invoke-DbgTest *dynamic*
Executes all tests whose name contains the word "dynamic".

.EXAMPLE
C:\> Invoke-DbgTest -Type PowerShell
Executes all PowerShell tests only.

.EXAMPLE
C:\> Invoke-DbgTest -Configuration Release
Executes tests on the Release build of DebugTools.

.LINK
Invoke-DbgBuild
Get-DbgTestResult
#>
function Invoke-DbgTest
{
    [CmdletBinding()]
    param(
        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory = $false, Position = 0)]
        [string[]]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateSet('C#', 'PowerShell')]
        [string[]]$Type,

        [Parameter(Mandatory = $false)]
        [Configuration]$Configuration = "Debug",

        [ValidateNotNullOrEmpty()]
        [Parameter(Mandatory = $false)]
        [string[]]$Tag,

        [Parameter(Mandatory = $false)]
        [switch]$Build
    )

    $testArgs = @{
        Name = $Name
        Type = $Type
        Configuration = $Configuration
        Tags = $Tag
        Build = $build
    }

    InvokeCSharpTest @testArgs
    InvokePowerShellTest @testArgs
}

function InvokeCSharpTest($name, $type, $configuration, $tags)
{
    if($type | HasType "C#")
    {
        $additionalArgs = @()

        $additionalArgs += GetLoggerArgs
        $additionalArgs += GetLoggerFilters $name $tags

        $testArgs = @{
            BuildFolder = Get-SolutionRoot
            AdditionalArgs = $additionalArgs
            Configuration = $configuration
        }

        $projectDir = Join-Path (Get-SolutionRoot) (Get-TestProject).Directory

        try
        {
            # Legacy vstest.console stores the test results in the TestResults folder under the current directory.
            # Change into the project directory whole we execute vstest to ensure the results get stored
            # in the right folder
            Push-Location $projectDir

            Invoke-CICSharpTest @testArgs -Verbose
        }
        finally
        {
            Pop-Location
        }
    }
}

function InvokePowerShellTest($name, $type, $configuration, $tags, $build)
{
    if($type | HasType "PowerShell")
    {
        $projectDir = Join-Path (Get-SolutionRoot) (Get-TestProject).Directory
        $testResultsDir = Join-Path $projectDir "TestResults"

        if(!(Test-Path $testResultsDir))
        {
            New-Item $testResultsDir -ItemType Directory | Out-Null
        }

        $dateTime = (Get-Date).ToString("yyyy-MM-dd_HH-mm-ss-fff")

        $additionalArgs = @{
            OutputFile = Join-Path $testResultsDir "DebugTools_PowerShell_$dateTime.xml"
            OutputFormat = "NUnitXml"
        }

        if($name -ne $null)
        {
            $additionalArgs.TestName = $name
        }

        if($null -ne $tags)
        {
            $additionalArgs.Tag = $tags
        }

        if(!$build -and $tag -notcontains "Build")
        {
            $additionalArgs.ExcludeTag = "Build"
        }

        $testArgs = @{
            BuildFolder = Get-SolutionRoot
            AdditionalArgs = $additionalArgs
        }

        Invoke-CIPowerShellTest @testArgs | Out-Null
    }
}

function GetLoggerArgs
{
    $loggerTarget = "trx;LogFileName=DebugTools_C#.trx"

    return "/logger:$loggerTarget"
}

function GetLoggerFilters($name, $tags)
{
    $filter = $null

    if($name)
    {
        $filter = ($name | foreach { "FullyQualifiedName~$($_.Trim('*'))" }) -join "|"
    }

    if($tags)
    {
        $tagsFilter = ($tags | foreach { "TestCategory=$($_)" }) -join "|"

        if($filter)
        {
            $filter = "($filter)&($tagsFilter)"
        }
        else
        {
            $filter = $tagsFilter
        }
    }

    if($filter)
    {
        return "/TestCaseFilter:$filter"
    }
}