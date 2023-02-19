function Invoke-AppveyorBeforeTest
{
    [CmdletBinding()]
    param()

    New-AppveyorPackage
}