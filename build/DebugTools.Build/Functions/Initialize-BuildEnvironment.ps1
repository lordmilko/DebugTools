$Host.UI.RawUI.WindowTitle = "DebugTools Build Environment"

function ShowBanner
{
    Write-Host "         Welcome to DebugTools Build Environment!"
    Write-Host ""
    Write-Host "  Build the latest version of DebugTools:                   " -NoNewLine
    Write-Host "Invoke-DbgBuild" -ForegroundColor Yellow

    Write-Host "  To find out what commands are available, type:            " -NoNewLine
    Write-Host "Get-DbgCommand" -ForegroundColor Yellow

    Write-Host "  Open a DebugTools prompt with:                            " -NoNewLine
    Write-Host "Start-DebugTools" -ForegroundColor Yellow

    Write-Host "  If you need more help, visit the DebugTools Wiki:         " -NoNewLine
    Write-Host "Get-DbgHelp" -ForegroundColor Yellow

    Write-Host ""
    Write-Host "          Copyright (C) lordmilko, 2022"
    Write-Host ""
    Write-Host ""
}

if(!$psISE)
{
    # Modify the prompt function to change the console prompt.
    function global:prompt{

        # change prompt text
        Write-Host "Dbg " -NoNewLine -ForegroundColor Green
        Write-Host ((Get-location).Path + ">") -NoNewLine
        return " "
    }
}

[Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", 1)

ShowBanner