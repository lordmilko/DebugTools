function Invoke-AppveyorTest
{
    [CmdletBinding()]
    param()

    Write-LogHeader "Executing tests"

    Invoke-AppveyorPesterTest
    Invoke-AppveyorCSharpTest
}

function Invoke-AppveyorPesterTest
{
    $result = Invoke-CIPowerShellTest $env:APPVEYOR_BUILD_FOLDER

    if($env:APPVEYOR)
    {
        foreach($test in $result.TestResult)
        {
            $appveyorTestArgs = @{
                Name = GetAppveyorTestName $test
                Framework = "Pester"
                Filename = "$($test.Describe).Tests.ps1"
                Outcome = GetAppveyorTestOutcome $test
                ErrorMessage = $test.FailureMessage
                Duration = [long]$test.Time.TotalMilliseconds
            }

            Add-AppveyorTest @appveyorTestArgs
        }
    }

    if($result.FailedCount -gt 0)
    {
        throw "$($result.FailedCount) Pester tests failed"
    }
}

function GetAppveyorTestName($test)
{
    $name = $test.Describe

    if(![string]::IsNullOrEmpty($test.Context))
    {
        $name += ": $($test.Context)"
    }

    $name += ": $($test.Name)"

    return $name
}

function GetAppveyorTestOutcome($test)
{
    switch($test.Result)
    {
        "Passed" { "Passed" }
        "Failed" { "Failed" }
        "Skipped" { "Skipped" }
        "Pending" { "NotRunnable" }
        "Inconclusive" { "Inconclusive" }
        default {
            throw "Test $(GetAppveyorTestName $test) completed with unknown result '$_'"
        }
    }
}

function Invoke-AppveyorCSharpTest
{
    $additionalArgs = @(
        "/TestCaseFilter:TestCategory!=SkipCI"
    )

    if($env:APPVEYOR)
    {
        $additionalArgs += "/logger:Appveyor"
    }

    Invoke-CICSharpTest $env:APPVEYOR_BUILD_FOLDER $additionalArgs
}