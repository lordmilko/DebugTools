function Invoke-AppveyorBeforeBuild
{
    [CmdletBinding()]
    param()

    Write-LogHeader "Restoring NuGet Packages"

    Invoke-Process { nuget restore (Join-Path $env:APPVEYOR_BUILD_FOLDER "DebugTools.sln") }
}