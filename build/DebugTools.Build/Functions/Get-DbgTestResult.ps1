<#
.SYNOPSIS
Retrieve test results from the last invocation of Invoke-DbgTest

.DESCRIPTION
The Get-DbgTestResult retrieves test results from the last invocation of Invoke-DbgTest. By default, test all test results for both C# and PowerShell can be displayed. Results can be limited to either of the two by specifying a value to the -Type parameter. The -Name parameter allows results to be further limited based on a wildcard expression that matches part of the results name. Results can also be filtered to those that had a particular status (such as Failed) using the -Outcome parameter.

To view results from one or more previous test invocations, the -ListAvailable parameter can be used to enumerate all available test files. These files can then be piped into Get-DbgTestResult to view their contents.

Note that whenever the Profiler.Tests project is built, all previous test results will automatically be cleared.

.PARAMETER Name
Wildcard specifying the tests to view the results of. If no value is specified, all test results will be displayed.

.PARAMETER Path
One or more test files to view the results of. Accepts values by pipeline.

.PARAMETER Type
Type of test results to view. By default both C# and PowerShell test results will be displayed.

.PARAMETER Outcome
Limits test results to only those with a specified outcome.

.PARAMETER ListAvailable
Lists all test files that are available within the test results directory.

.EXAMPLE
C:\> Get-DbgTestResult
View all test results

.EXAMPLE
C:\> Get-DbgTestResult *dynamic*
View all test results whose name contains the word "dynamic"

.EXAMPLE
C:\> Get-DbgTestResult -Outcome Failed
View all tests that failed in the last invocation of Invoke-DbgTest

.EXAMPLE
C:\> Get-DbgTestResult -ListAvailable
List all unit test results that are available

.EXAMPLE
C:\> Get-DbgTestResult *2019* -ListAvailable | Get-DbgTestResult *dynamic* -Type C#
Get all C# test results from 2019 whose test name contains the word "dynamic"

.LINK
Invoke-DbgTest
#>
function Get-DbgTestResult
{
    [CmdletBinding(DefaultParameterSetName = "Default")]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Name = "*",

        [Parameter(Mandatory = $false, ParameterSetName = "Default", ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
        [string[]]$Path,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [ValidateSet('C#', 'PowerShell')]
        [string[]]$Type,

        [Parameter(Mandatory = $false, ParameterSetName = "Default")]
        [ValidateSet("Success", "Failed")]
        [string]$Outcome,

        [Parameter(Mandatory = $false, ParameterSetName = "ListAvailable")]
        [switch]$ListAvailable
    )

    Process {
        if([string]::IsNullOrWhiteSpace($Name))
        {
            $Name = "*"
        }

        $outcomeInternal = $Outcome

        if([string]::IsNullOrEmpty($outcomeInternal))
        {
            $outcomeInternal = "*"
        }

        switch($PSCmdlet.ParameterSetName)
        {
            "Default" {

                $resultArgs = @{
                    Type = $Type
                    Path = $Path
                }

                GetCSharpTestResults @resultArgs | where { $_.Name -Like $Name -and $_.Outcome -like $outcomeInternal } | Sort-Object Name
                GetPowerShellTestResults @resultArgs | where { $_.Name -like $Name -and $_.Outcome -like $outcomeInternal } | Sort-Object Name
            }

            "ListAvailable" {
                ListAvailable $Name
            }
        }
    }
}

function ListAvailable($name, $warn = $false)
{
    $projectDir = Join-Path (Get-SolutionRoot) (Get-TestProject $true).Directory
    $testResultsDir = Join-Path $projectDir "TestResults"

    if(!(Test-Path $testResultsDir))
    {
        throw "Unable to retrieve test results as test results folder '$testResultsDir' does not exist"
    }

    # Specify * so the FileInfo.ToString() returns the full path of the item.
    # https://stackoverflow.com/questions/18186841/converting-get-childitem-output-to-string
    $results = gci (Join-Path $testResultsDir "*") -Filter $name

    if(!$results)
    {
        if($warn)
        {
            Write-Warning "Unable to retrieve $($name.Trim('*')) results as no test files are available."
        }
        else
        {
            throw "Unable to retrieve test results as no test result files exist in folder '$testResultsDir' that match the wildcard '$name'"
        }
    }

    return $results
}

function GetCSharpTestResults($type, $path)
{
    if(!($type | HasType "C#"))
    {
        return
    }

    if(!$path)
    {
        $path = ListAvailable "*C#*" $true |Sort-Object lastwritetime -Descending|select -First 1

        if(!$path)
        {
            return
        }
        else
        {
            $path = $path.FullName
        }
    }
    else
    {
        $path = $path | where { (Split-Path $_ -leaf) -like "*C#*" }
    }

    foreach($p in $path)
    {
        [xml]$xml = gc $p

        # Extract a set of test results from the items in a trx file
        $results = $xml.TestRun.Results.UnitTestResult

        foreach($result in $results)
        {
            $obj = [PSCustomObject]@{
                Name = $result.testName
                Outcome = GetOutcome $result.outcome
                Duration = [TimeSpan]::FromMilliseconds([int]([TimeSpan]::Parse($result.duration).TotalMilliseconds))
                Type = "C#"
                Message = $result.Output.ErrorInfo.Message
                StackTrace = $result.Output.ErrorInfo.StackTrace
                File = $p
            }

            $obj.PSObject.TypeNames.Add("DebugTools.Build.TestResultPSObject")

            $obj
        }
    }
}

function GetPowerShellTestResults($type, $path)
{
    if(!($type | HasType "PowerShell"))
    {
        return
    }

    if(!$path)
    {
        $path = ListAvailable "*PowerShell*" $true | Sort-Object lastwritetime -Descending|select -First 1

        if(!$path)
        {
            return
        }
        else
        {
            $path = $path.FullName
        }
    }
    else
    {
        $path = $path | where { (Split-Path $_ -leaf) -like "*PowerShell*" }
    }

    foreach($p in $path)
    {
        [xml]$xml = gc $p

        ProcessTestSuite $xml.'test-results' $p
    }
}

function ProcessTestSuite($parent, $filePath)
{
    foreach($suite in $parent.'test-suite'.results)
    {
        ProcessTestSuite $suite $filePath
        ProcessTestCase $suite $filePath
    }
}

function ProcessTestCase($suite, $filePath)
{
    foreach($case in $suite.'test-case')
    {
        $period = $case.name.IndexOf(".")

        $name = $case.name

        if($period -ne -1)
        {
            $name = $case.name.Remove($period, 1).Insert($period, ": ")
        }

        $obj = [PSCustomObject]@{
            Name = $name
            Outcome = GetOutcome $case.result
            Duration = [TimeSpan]::FromSeconds([math]::Round($case.time, 3))
            Type = "PS"
            Message = $case.failure.message
            StackTrace = $case.failure.'stack-trace'
            File = $filePath
        }

        $obj.PSObject.TypeNames.Add("DebugTools.Build.TestResultPSObject")

        $obj
    }
}

function GetOutcome($outcome)
{
    switch($outcome)
    {
        { $_ -in "Success","Passed" } {
            "Success"
        }
        { $_ -in "Failure","Failed" } {
            "Failed"
        }
        default {
            throw "Don't know how to format outcome '$_'"
        }
    }
}