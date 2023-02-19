param(
    [Parameter(Mandatory = $false)]
    [bool]$ShowWelcome = $false
)

ipmo $PSScriptRoot\..\CI\ci.psm1 -Scope Local
ipmo $PSScriptRoot\..\CI\Appveyor.psm1 -Scope Local -DisableNameChecking

. $PSScriptRoot\..\CI\Helpers\Import-ModuleFunctions.ps1
. Import-ModuleFunctions "$PSScriptRoot\Functions" -Exclude "Initialize-BuildEnvironment*"

if($ShowWelcome)
{
    . $PSScriptRoot\Functions\Initialize-BuildEnvironment.ps1
}

enum Configuration {
    Debug = 0
    Release = 1
}

New-Alias Simulate-DbgCI Test-DbgCI

function Write-DbgProgress
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Activity,

        [Parameter(Mandatory = $true, Position = 1)]
        [string]$Status,

        [Parameter(Mandatory = $false)]
        [int]$PercentComplete
    )

    $global:dbgProgressArgs = @{
        Activity = $Activity
        Status = $Status
        PercentComplete = $PercentComplete
    }

    Write-Progress @global:DbgProgressArgs
}

function Complete-DbgProgress
{
    if($global:dbgProgressArgs)
    {
        Write-Progress @global:DbgProgressArgs -Completed
        $global:dbgProgressArgs = $null
    }    
}

Export-ModuleMember Test-DbgCI -Alias Simulate-DbgCI