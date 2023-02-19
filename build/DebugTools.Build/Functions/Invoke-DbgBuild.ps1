<#
.SYNOPSIS
Compiles DebugTools from source

.DESCRIPTION
The Invoke-DbgBuild cmdlet compiles DebugTools from source. By default, all projects in the DebugTools solution will uilt using the Debug configuration. A specific project can be built by specifying a wildcard expression to the -Name parameter.

In the event you wish to debug your build, the -Dbg parameter can be specified. This will generate a *.binlog file in the root of the project solution that will be automatically opened in the MSBuild Structured Log Viewer when the build has completed (assuming it is installed).

.PARAMETER Name
Wildcard specifying the name of a single DebugTools project to build. If no value is specified, the entire DebugTools solution will be built.

.PARAMETER ArgumentList
Additional arguments to pass to the build tool.

.PARAMETER Configuration
Configuration to build. If no value is specified, DebugTools will be built for Debug.

.PARAMETER DebugBuild
Specifies whether to generate an msbuild *.binlog file. File will automatically be opened upon completion of the build.

.PARAMETER SourceLink
Specifies whether to build DebugTools with SourceLink debug info. If this value is not specified, it will be true by default.

.PARAMETER ViewLog
Specifies whether to open the debug log upon finishing the build when -DebugBuild is specified.

.EXAMPLE
C:\> Invoke-DbgBuild
Build a Debug version of DebugTools

.EXAMPLE
C:\> Invoke-DbgBuild -c Release
Build a Release version of DebugTools

.EXAMPLE
C:\> Invoke-DbgBuild *powershell*
Build just the DebugTools.PowerShell project of DebugTools

.EXAMPLE
C:\> Invoke-DbgBuild -Dbg
Build DebugTools and log to a *.binlog file to be opened by the MSBuild Structured Log Viewer upon completion

.LINK
Clear-DbgBuild
Invoke-DbgTest
#>
function Invoke-DbgBuild
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Name,

        [Alias("Args")]
        [Parameter(Mandatory = $false, Position = 1)]
        [string[]]$ArgumentList,

        [Parameter(Mandatory = $false)]
        [Configuration]$Configuration = "Debug",

        [Alias("Dbg")]
        [Alias("DebugMode")]
        [Parameter(Mandatory = $false)]
        [switch]$DebugBuild,

        [Parameter(Mandatory = $false)]
        [switch]$SourceLink,

        [Parameter(Mandatory = $false)]
        [switch]$ViewLog = $true
    )

    # On Linux you need to have libcurl and some other stuff for libgit2 to work properly;
    # users don't care about that, and don't need to include SourceLink anyway so just skip it
    if(($SourceLink -or !$PSBoundParameters.ContainsKey("SourceLink")))
    {
        $SourceLink = $true
    }

    $splattedArgs = @{
        Configuration = $Configuration
        SourceLink = $SourceLink
    }

    if($Name)
    {
        $candidates = Get-BuildProject

        $projects = $candidates | where { $_.BaseName -like $Name }

        if(!$projects)
        {
            throw "Cannot find any projects that match the wildcard '$Name'. Please specify one of $(($candidates|select -expand BaseName) -join ", ")"
        }

        if($projects.Count -gt 1)
        {
            $str = ($projects|select -ExpandProperty BaseName|Sort-Object) -join ", "
            throw "Can only specify one project at a time, however wildcard '$Name' matched multiple projects: $str"
        }

        $splattedArgs.Target = $projects.FullName
    }

    $root = Get-SolutionRoot

    $additionalArgs = @()

    if($ArgumentList -ne $null)
    {
        $additionalArgs += $ArgumentList
    }

    if($DebugBuild)
    {
        $binLog = Join-Path $root "msbuild.binlog"

        $additionalArgs += "/bl:$binLog"
    }

    $splattedArgs.BuildFolder = $root
    $splattedArgs.AdditionalArgs = $additionalArgs

    Invoke-CIRestoreFull $root -Verbose

    try
    {
        Invoke-CIBuild @splattedArgs -Verbose
    }
    finally
    {
        if($DebugBuild -and $ViewLog)
        {
            Start-Process $binLog
        }
    }
}