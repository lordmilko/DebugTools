function Invoke-CITest
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $BuildFolder,

        [Parameter(Position = 1)]
        $AdditionalArgs,

        [Parameter(Mandatory = $false)]
        $Configuration = $env:CONFIGURATION
    )

    Invoke-CIPowerShellTest $BuildFolder $AdditionalArgs
    Invoke-CICSharpTest $BuildFolder $AdditionalArgs $Configuration
}

function Invoke-CICSharpTest
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $BuildFolder,

        [Parameter(Position = 1)]
        $AdditionalArgs,

        [Parameter(Mandatory = $false, Position = 2)]
        $Configuration = $env:CONFIGURATION
    )

    Write-LogInfo "`tExecuting C# tests"

    $testProjectDetails = Get-TestProject

    $ch = [IO.Path]::DirectorySeparatorChar

    $dll = Join-Path $BuildFolder "artifacts\bin\$Configuration\$($testProjectDetails.Directory.Replace("src$ch",'')).dll"
    Write-Verbose "Using DLL '$dll'"

    Invoke-CICSharpTestFull $dll $BuildFolder $Configuration $AdditionalArgs
}

function Invoke-CICSharpTestFull($dll, $BuildFolder, $Configuration, $AdditionalArgs)
{
    $vsTestArgs = @(
        $dll
    )

    if($AdditionalArgs)
    {
        $vsTestArgs += $AdditionalArgs
    }

    $vstest = Get-VSTest

    Write-Verbose "Executing command $vstest $vsTestArgs"

    Invoke-Process {
        & $vstest $vsTestArgs
    } -WriteHost
}

function Invoke-CIPowerShellTest
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $BuildFolder,

        [Parameter(Position = 1)]
        $AdditionalArgs
    )

    $relativePath = (Get-TestProject).PowerShell

    $directory = Join-Path $BuildFolder $relativePath

    if(!(Test-Path $directory))
    {
        Write-LogError "Cannot run PowerShell tests: directory $relativePath does not exist"
        return
    }

    Write-LogInfo "`tExecuting PowerShell tests"

    Install-CIDependency Pester

    Invoke-Pester $directory -PassThru @AdditionalArgs
}