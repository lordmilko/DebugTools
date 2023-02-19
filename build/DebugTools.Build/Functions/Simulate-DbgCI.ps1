<#
.SYNOPSIS
Simulates building DebugTools under a Continuous Integration environment

.DESCRIPTION
The Simulate-DbgCI simulates the entire workflow of building DebugTools under Appveyor. By default, Simulate-DbgCI will invoke all steps that would normally be performed as part of the CI process. This can be limited by specifying a specific list of tasks that should be simulated via the -Task parameter.

.PARAMETER Appveyor
Specifies to simulate Appveyor CI

.PARAMETER Task
CI task to execute. If no value is specified, all CI tasks will be executed.

.EXAMPLE
C:\> Simulate-DbgCI
Simulate Appveyor CI

.EXAMPLE
C:\> Simulate-DbgCI -Task Test
Simulate Appveyor CI tests
#>
function Test-DbgCI
{
    [CmdletBinding(DefaultParameterSetName = "Appveyor")]
    param(
        [Parameter(Mandatory = $false, ParameterSetName = "Appveyor")]
        [switch]$Appveyor,

        [Parameter(Mandatory = $false, Position = 0, ParameterSetName="Appveyor")]
        [ValidateSet("Install", "Restore", "Build", "Package", "Test", "Coverage")]
        [string[]]$Task,

        [Parameter(Mandatory=$false)]
        [Configuration]$Configuration = "Debug"
    )

    switch($PSCmdlet.ParameterSetName)
    {
        "Appveyor" {

            if($null -eq $Task)
            {
                Simulate-Appveyor -Configuration $Configuration
            }
            else
            {
                Simulate-Environment {
                    if("Install" -in $Task) {
                        Invoke-AppveyorInstall
                    }
                    if("Restore" -in $Task) {
                        Invoke-AppveyorBeforeBuild
                    }
                    if("Build" -in $Task) {
                        Invoke-AppveyorBuild
                    }
                    if("Package" -in $Task) {
                        Invoke-AppveyorBeforeTest
                    }
                    if("Test" -in $Task) {
                        Invoke-AppveyorTest
                    }
                    if("Coverage" -in $Task)
                    {
                        Invoke-AppveyorAfterTest
                    }
                } -Configuration $Configuration
            }
        }
    }
}