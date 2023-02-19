ipmo $PSScriptRoot\ci.psm1 -Scope Local

$script:SolutionDir = $script:SolutionDir = Get-SolutionRoot

. $PSScriptRoot\Helpers\Import-ModuleFunctions.ps1
. Import-ModuleFunctions "$PSScriptRoot\Appveyor"

function Enable-AppveyorRDPAccess
{
    $blockRdp = $true; iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/appveyor/ci/master/scripts/enable-rdp.ps1'))
}

function GetVersion
{
    return (Get-CIVersion $env:APPVEYOR_BUILD_FOLDER).File.ToString(3)
}

Export-ModuleMember Enable-AppveyorRDPAccess,Simulate-Environment