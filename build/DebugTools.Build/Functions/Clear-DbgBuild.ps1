<#
.SYNOPSIS
Clears the output of one or more previous DebugTools builds.

.DESCRIPTION
The Clear-DbgBuild clears the output of previous builds of DebugTools. By default, Clear-DbgBuild will attempt to use MSBuild to clear the previous build. If If -Full is specified, Clear-DbgBuild will will instead force remove the bin and obj folders of each project in the solution.

.PARAMETER Configuration
Configuration to clean. If no value is specified DebugTools will clean the last Debug build.

.PARAMETER Full
Specifies whether to brute force remove all build and object files in the solution.

.EXAMPLE
C:\> Clear-DbgBuild
Clear the last build of DebugTools

.EXAMPLE
C:\> Clear-DbgBuild -Full
Remove all obj and bin folders under each project of DebugTools

.LINK
Invoke-DbgBuild
#>
function Clear-DbgBuild
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [ValidateSet("Debug", "Release")]
        $Configuration = "Debug",

        [Parameter(Mandatory = $false)]
        [switch]$Full
    )

    $devenv = Get-Process devenv -ErrorAction SilentlyContinue

    if($devenv)
    {
        Write-LogError "Warning: Visual Studio is currently running. Some items may not be able to be removed"
    }

    $root = Get-SolutionRoot

    $binLog = Join-Path $root "msbuild.binlog"

    if(Test-Path $binLog)
    {
        Remove-Item $binLog -Force
    }

    if($Full)
    {
        $projects = gci $root -Recurse -Filter *.csproj

        foreach($project in $projects)
        {
            Write-LogInfo "Processing $project"

            $folder = Split-Path $project.FullName -Parent

            $bin = Join-Path $folder "bin"
            $obj = Join-Path $folder "obj"
            $artifacts = Join-Path $folder "artifacts"

            if(Test-Path $bin)
            {
                Write-LogError "`tRemoving $bin"
                RemoveItems $bin
            }

            if(Test-Path $obj)
            {
                # obj will be automatically recreated and removed each time Clear-DbgBuild is run,
                # due to dotnet/msbuild clean recreating it
                Write-LogError "`tRemoving $obj"
                RemoveItems $obj
            }

            if(Test-Path $artifacts)
            {
                Write-LogError "`tRemoving $artifacts"
                RemoveItems $artifacts
            }
        }

        $artifacts = Join-Path $root "artifacts"

        if(Test-Path $artifacts)
        {
            Write-LogError "`tRemoving $artifacts"
            RemoveItems $artifacts
        }

        Write-LogInfo "Processing Redistributable Packages"

        $clearArgs = @{
            BuildFolder = $root
            Configuration = $Configuration
            NuGetOnly = $true
        }

        Clear-CIBuild @clearArgs
    }
    else
    {
        $clearArgs = @{
            BuildFolder = $root
            Configuration = $Configuration
        }

        Clear-CIBuild @clearArgs -Verbose
    }
}

function RemoveItems($folder)
{
    $items = gci $folder -Recurse

    $files = $items | where { !$_.PSIsContainer }

    foreach($file in $files)
    {
        Write-LogError "`t`tRemoving '$file'"

        $file | Remove-Item -Force
    }

    $folders = $items | where { $_.PSIsContainer }

    foreach($f in $folders)
    {
        if(Test-Path $f)
        {
            Write-LogError "`t`tRemoving '$f'"

            $f | Remove-Item -Force -Recurse
        }
    }

    $folder | Remove-Item -Force -Recurse
}