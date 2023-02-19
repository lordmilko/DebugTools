<#
.SYNOPSIS
Opens the DebugTools Wiki for getting help with the DebugTools Build Environment.

.DESCRIPTION
The Get-DbgHelp cmdlet opens the DebugTools Wiki page containing detailed instructions on compiling DebugTools and using the DebugTools Build Environment.

.EXAMPLE
C:\> Get-DbgHelp
Open the DebugTools Wiki article detailing how to compile DebugTools.
#>
function Get-DbgHelp
{
    [CmdletBinding()]
    param()

    $url = "https://github.com/lordmilko/DebugTools/wiki/Build-Environment"

    Start-Process $url
}