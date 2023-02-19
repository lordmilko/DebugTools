function Simulate-Appveyor
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$Configuration = "Debug"
    )

    if($env:APPVEYOR)
    {
        throw "Simulate-Appveyor should not be run from within Appveyor"
    }

    InitializeEnvironment $configuration

    Clear-AppveyorBuild

    Invoke-AppveyorInstall     # install            Install Chocolatey packages, NuGet provider for NuGet testing
    Invoke-AppveyorBeforeBuild # before_build       Restore NuGet packages
    Invoke-AppveyorBuild       # build_script       Build for all target frameworks
    Invoke-AppveyorAfterBuild  # after_build        Set Appveyor build from DebugTools version
    Invoke-AppveyorBeforeTest  # before_test        Build/test NuGet
    Invoke-AppveyorTest        # test_script        Test .NET and Pester
    Invoke-AppveyorAfterTest   # after_test         .NET Coverage
}

function Simulate-Environment
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [ScriptBlock]$ScriptBlock,

        [Parameter(Mandatory = $false)]
        [string]$Configuration = "Debug"
    )

    InitializeEnvironment $Configuration

    & $ScriptBlock
}

function InitializeEnvironment($configuration)
{
    $env:CONFIGURATION = $configuration
    $env:APPVEYOR_BUILD_FOLDER = $script:SolutionDir
    $env:APPVEYOR_REPO_COMMIT_MESSAGE = 'Did some stuff'
    $env:APPVEYOR_REPO_COMMIT_MESSAGE_EXTENDED = 'For #4'
    $env:APPVEYOR_ACCOUNT_NAME = "lordmilko"
    $env:APPVEYOR_PROJECT_SLUG = "DebugTools"
}