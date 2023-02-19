<#
.SYNOPSIS
Starts a new PowerShell console containing the compiled version of DebugTools.

.DESCRIPTION
The Start-DebugTools starts starts a previously compiled version of DebugTools in a new PowerShell console. By default, Start-DebugTools will attempt to launch the last Debug build of DebugTools.

.PARAMETER Configuration
Build configuration to launch. If no configuration is specified, Debug will be used.

.EXAMPLE
C:\> Start-DebugTools
Open a PowerShell console containing the only target framework that has been compiled

.LINK
Invoke-DbgBuild
#>
function Start-DebugTools
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [Configuration]$Configuration = "Debug"
    )

    $path = GetModulePath $Configuration

    if(Test-Path $path)
    {
        $exe = GetPowerShellExe $path

        $psd1 = Join-PathEx $path DebugTools DebugTools.psd1

        Write-Host -ForegroundColor Green "`nLaunching DebugTools from '$psd1'`n"

        Start-Process $exe -ArgumentList "-executionpolicy","bypass","-noexit","-command","ipmo $psd1; cd ~"
    }
    else
    {
        throw "Cannot start DebugTools: solution has not been compiled for '$Configuration' build. Path '$path' does not exist."
    }
}

function GetPowerShellExe($path)
{
    return "powershell"    
}

function GetModulePath($configuration)
{
    $root = Get-SolutionRoot

    $bin = Join-PathEx $root artifacts bin

    $targetFolder = CalculateTargetFolder

    return $targetFolder
}

function CalculateTargetFolder
{
    return Join-Path $bin $configuration
}