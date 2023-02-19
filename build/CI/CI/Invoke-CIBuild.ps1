function Invoke-CIBuild
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $BuildFolder,

        [Parameter(Position = 1)]
        $AdditionalArgs,

        [Parameter(Mandatory = $false, Position = 2)]
        $Configuration = $env:CONFIGURATION,

        [Parameter(Mandatory = $false)]
        $Target,

        [Parameter(Mandatory = $false)]
        [switch]$SourceLink
    )

    if([string]::IsNullOrEmpty($Target))
    {
        $Target = Join-Path $BuildFolder "DebugTools.sln"
    }

    $innerArgs = @{
        BuildFolder = $BuildFolder
        AdditionalArgs = $AdditionalArgs
        Configuration = $Configuration
        Target = $Target
        SourceLink = $SourceLink
    }

    Invoke-CIBuildFull @innerArgs
}

function Invoke-CIBuildFull
{
    param(
        $BuildFolder,
        $AdditionalArgs,
        $Configuration,
        $Target,
        $SourceLink
    )

    $msbuild = Get-MSBuild

    $msbuildArgs = @(
        $Target
        "/verbosity:minimal"
        "/p:Configuration=$Configuration"
    )

    if($SourceLink)
    {
        $msbuildArgs += "/p:EnableSourceLink=true"
    }

    if($AdditionalArgs)
    {
        $msbuildArgs += $AdditionalArgs
    }

    Write-Verbose "Executing command '$msbuild $msbuildArgs'"

    Invoke-Process {
        & $msbuild @msbuildArgs
    } -WriteHost
}

function Invoke-CIRestoreFull
{
    [CmdletBinding()]
    param($root)

    Install-CIDependency nuget

    $nuget = Get-ChocolateyCommand nuget
    $sln = Join-Path $root "DebugTools.sln"

    $nugetArgs = @(
        "restore"
        $sln
    )

    Write-Verbose "Executing command '$nuget $nugetArgs'"

    Invoke-Process { & $nuget $nugetArgs } -WriteHost
}