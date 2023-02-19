<#
.SYNOPSIS
Continuously retrieves logs emitted by the DebugTools Build Environment

.DESCRIPTION
The Get-DbgLog cmdlet continuously retrieves logs emitted by cmdlets used to build DebugTools. By default, Get-DbgLog will retrieve logs emitted by all cmdlets in the DebugTools Build Environment.

When invoked, Get-DbgLog will begin continuously tailing the end of the specified log file, showing any new entries in the window where Get-DbgLog was invoked. Get-DbgLog can filter the entries emitted to the console by specifying a pattern to the -Pattern parameter. If -Clear is specified, the contents of the specified log will will be removed before Get-DbgLog begins streaming.

If you wish to view logs in a separate window from where Get-DbgLog was invoked, you can do so by specifying the -NewWindow parameter. If you wish to view the entirety of the specified log, you can specify the -Full parameter to open the log file in your default text editor.

.PARAMETER Pattern
A pattern to use to filter log entries emitted to the console.

.PARAMETER Full
Opens the log file in your default text editor, rather than streaming the end of the file to the console.

.PARAMETER Lines
Number of lines to display from the end of the log file when Get-DbgLog is first invoked. By default this value is 10

.PARAMETER Clear
Clear the specified log before viewing it

.PARAMETER NewWindow
Opens the log in a new window.

.EXAMPLE
C:\> Get-DbgLog
View the Build log file

.EXAMPLE
C:\> Get-DbgLog -Full
Open the Build log file in a text editor

.EXAMPLE
C:\> Get-DbgLog -Clear
Clear and view the Build log file

.LINK
Invoke-DbgTest
#>
function Get-DbgLog
{
    [CmdletBinding(
        DefaultParameterSetName = "Build"
    )]
    param(
        [Parameter(Mandatory = $false, Position = 0, ParameterSetName = "Build")]
        [string[]]$Pattern,

        [Parameter(Mandatory = $true, ParameterSetName = "BuildFull")]
        [switch]$Full = $false,

        [Parameter(Mandatory = $false, ParameterSetName = "Build")]
        [int]$Lines = 10,

        [Parameter(Mandatory = $false)]
        [switch]$Clear = $false,
        
        [Alias("Window")]
        [Parameter(Mandatory = $false, ParameterSetName = "Build")]
        [switch]$NewWindow
    )

    $log = GetLogFile $PSCmdlet.ParameterSetName

    if(Test-Path $log)
    {
        if($Clear)
        {
            Set-Content $log "" -NoNewline
        }

        if($Full)
        {
            Start-Process $log
        }
        else
        {
            $expr = $null

            if($Pattern)
            {
                $expr = "gc '$log' -Tail $Lines -Wait | sls $Pattern"
            }
            else
            {
                $expr = "gc '$log' -Tail $Lines -Wait"
            }

            if($NewWindow)
            {
                $powerShell = [system.diagnostics.process]::GetCurrentProcess().MainModule.filename

                $expr = "`$Host.UI.RawUI.WindowTitle = '$log'; $expr"

                Start-Process $powerShell "-Command",$expr
            }
            else
            {
                cls
                $Host.UI.RawUI.WindowTitle = $log

                Invoke-Expression $expr
            }
        }
    }
    else
    {
        Write-Host -ForegroundColor Red "$log does not exist"
    }
}

function GetLogFile($parameterSetName)
{
    $temp = [IO.Path]::GetTempPath()

    if($parameterSetName -like "Build*")
    {
        return Join-Path $temp "DebugTools.Build.log"
    }
    else
    {
        throw "Don't know what log to retrieve for parameter set '$parameterSetName'"
    }
}