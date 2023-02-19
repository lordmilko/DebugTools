function Measure-AppveyorCoverage
{
    [CmdletBinding()]
    param()

    Write-LogHeader "Calculating code coverage"

    Get-CodeCoverage -Configuration $env:CONFIGURATION

    $lineCoverage = Get-LineCoverage

    $threshold = 95.3

    if($lineCoverage -lt $threshold)
    {
        $msg = "Code coverage was $lineCoverage%. Coverage must be higher than $threshold%"

        Write-LogError $msg

        throw $msg
    }
    else
    {
        Write-LogInfo "`tCoverage report completed with $lineCoverage% code coverage"

        if($env:APPVEYOR)
        {
            Write-LogInfo "`tUploading coverage to codecov"
            Invoke-Process { cmd /c "codecov -f `"$env:temp\opencover.xml`" 2> nul" } -WriteHost
        }
    }
}