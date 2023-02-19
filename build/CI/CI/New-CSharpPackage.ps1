function New-CSharpPackage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$BuildFolder,

        [Parameter(Mandatory = $true)]
        [string]$OutputFolder,

        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$Configuration
    )

    Write-LogInfo "`t`tBuilding package"

    $nuget = $null
    $nugetArgs = $null

    $nugetArgs = @(
        "pack"
        Join-Path $BuildFolder "src\DebugTools\DebugTools.csproj"
        "-Exclude"
        "**/*.tt;**/*.txt;**/*.json"
        "-outputdirectory"
        "$OutputFolder"
        "-NoPackageAnalysis"
        "-symbols"
        "-SymbolPackageFormat"
        "snupkg"
        "-version"
        $Version
        "-properties"
        "Configuration=$Configuration"
    )

    Install-CIDependency nuget

    $nuget = "nuget"

    Write-Verbose "Executing command '$nuget $nugetArgs'"

    Invoke-Process { & $nuget @nugetArgs }
}