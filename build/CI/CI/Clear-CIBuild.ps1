function Clear-CIBuild
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        $BuildFolder,

        [Parameter(Mandatory = $false, Position = 1)]
        $Configuration = $env:CONFIGURATION,

        [switch]$NuGetOnly
    )

    if(!$NuGetOnly)
    {
        $path = Join-Path $BuildFolder DebugTools.sln

        $msbuild = Get-MSBuild

        $msbuildArgs = @(
            "/t:clean"
            "`"$path`""
            "/p:Configuration=$Configuration"
        )

        Write-Verbose "Executing command '$msbuild $msbuildArgs'"

        Invoke-Process { & $msbuild @msbuildArgs } -WriteHost
    }

    $nupkgs = @(gci $BuildFolder -Recurse -Filter *.*nupkg | where {
        !$_.FullName.StartsWith((Join-Path $BuildFolder "packages"))
    })
    
    $nupkgs += (gci $BuildFolder -Filter *.zip)
    
    $nupkgs | foreach {
        Write-LogError "`tRemoving $($_.FullName)"

        $_ | Remove-Item -Force
    }
}