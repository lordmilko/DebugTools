function Clear-AppveyorBuild
{
    [CmdletBinding()]
    param(
        [switch]$NuGetOnly
    )

    Write-LogHeader "Cleaning Appveyor build folder"

    $clearArgs = @{
        BuildFolder = $env:APPVEYOR_BUILD_FOLDER
        Configuration = $env:CONFIGURATION
        NuGetOnly = $NuGetOnly
    }

    Clear-CIBuild @clearArgs
}