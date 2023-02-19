function Write-LogHeader($msg)
{
    if($env:APPVEYOR)
    {
        Write-LogInfo $msg
    }
    else
    {
        Write-Log $msg Cyan
    }
}

function Write-LogSubHeader($msg)
{
    Write-Log $msg Magenta
}

function Write-LogInfo($msg)
{
    Write-Log $msg
}

function Write-LogError($msg)
{
    Write-Log $msg Yellow
}

function Write-Log($msg, $color)
{
    if($global:dbgBuildDisableLog)
    {
        return
    }

    $msg = "`t$msg"

    $msg = $msg -replace "`t","    "

    if(!$global:dbgProgressArgs)
    {
        if($color)
        {
            Write-Host -ForegroundColor $color $msg
        }
        else
        {
            Write-Host $msg
        }
    }
    else
    {
        $global:dbgProgressArgs.CurrentOperation = $msg.Trim()
        Write-Progress @global:dbgProgressArgs
    }

    $nl = [Environment]::NewLine

    $path = Join-Path ([IO.Path]::GetTempPath()) "DebugTools.Build.log"

    [IO.File]::AppendAllText($path, "$(Get-Date) $msg$nl")
}

function Write-LogVerbose($msg, $color)
{
    if($psISE)
    {
        Write-Verbose $msg

        $msg = "`t$msg"

        $msg = $msg -replace "`t","    "

        $nl = [Environment]::NewLine

        $path = Join-Path ([IO.Path]::GetTempPath()) "DebugTools.Build.log"

        [IO.File]::AppendAllText($path, "$(Get-Date) $msg$nl")
    }
    else
    {
        Write-Log $msg $color
    }
}