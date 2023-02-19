function Invoke-AppveyorInstall
{
    [CmdletBinding()]
    param()

    Write-LogHeader "Installing build dependencies"

    Install-CIDependency -Log
}