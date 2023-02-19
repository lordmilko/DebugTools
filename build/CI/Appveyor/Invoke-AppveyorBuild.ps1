function Invoke-AppveyorBuild
{
    [CmdletBinding()]
    param()

    Write-LogHeader "Building DebugTools"

    $additionalArgs = @()

    if($env:APPVEYOR)
    {
        $additionalArgs += "/logger:`"C:\Program Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll`""
    }

    Invoke-CIBuild $env:APPVEYOR_BUILD_FOLDER $additionalArgs -SourceLink
}