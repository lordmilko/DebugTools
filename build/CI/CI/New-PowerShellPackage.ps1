function New-PowerShellPackage
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$OutputDir,

        [Parameter(Mandatory = $true)]
        $RepoManager,

        [Parameter(Mandatory = $true)]
        [string]$Configuration,

        [Parameter(Mandatory = $true)]
        [switch]$Redist,

        [Parameter(Mandatory = $false)]
        [switch]$PowerShell
    )

    $dll = Join-Path $OutputDir "DebugTools.PowerShell.dll"

    if(!(Test-Path $dll))
    {
        throw "Cannot build PowerShell package as DebugTools has not been compiled. Could not find file '$dll'."
    }

    $RepoManager.WithTempCopy(
        $OutputDir,
        {
            param($tempPath)

            New-RedistributablePackage $tempPath $OutputDir $Configuration $Redist

            if($PowerShell)
            {
                $modulePath = $tempPath

                New-PowerShellPackageInternal $modulePath
            }            
        }
    )
}

function New-RedistributablePackage($tempPath, $outputDir, $configuration, $redist)
{
    $packageDir = $tempPath

    if($redist)
    {
        $packageDir = Join-Path $packageDir "*"

        $destinationPath = Join-Path (PackageManager -RepoLocation) "DebugTools.zip"

        if(Test-Path $destinationPath)
        {
            Remove-Item $destinationPath -Force
        }

        try
        {
            $global:ProgressPreference = "SilentlyContinue"

            Compress-Archive $packageDir -DestinationPath $destinationPath
        }
        finally
        {
            $global:ProgressPreference = "Continue"
        }
    }
}

function New-PowerShellPackageInternal($modulePath)
{
    # Remove any files that are not required in the nupkg

    $list = @(
        "*.cmd"
        "*.pdb"
        "*.json"
        "*.nupkg"
        "*.exp"
        "*.lib"
        "DebugTools.xml"
        "DebugTools.PowerShell.xml"
        "ChaosLib.xml"
        "ClrDebug.xml"
        "DebugTools.Host.*.config"
    )

    gci $modulePath -Include $list -Recurse | Remove-Item -Force

    Publish-PowerShellPackage $modulePath
}

function Publish-PowerShellPackage($tempPath)
{
    Write-LogInfo "`t`tPublishing module to $(PackageManager -RepoName)"

    $expr = "try { `$global:ProgressPreference = 'SilentlyContinue'; Publish-Module -Path '$tempPath' -Repository $(PackageManager -RepoName) -WarningAction SilentlyContinue } finally { `$global:ProgressPreference = 'Continue' }"

    $expr = $expr -replace "Publish-Module","Publish-ModuleEx"

    Write-Verbose "Executing '$expr'"
    Invoke-Expression $expr
}

function Publish-ModuleEx
{
    [CmdletBinding()]
    param(
        [string]$Path,
        [string]$Repository
    )

    Get-CallerPreference $PSCmdlet $ExecutionContext.SessionState

    Publish-Module -Path $Path -Repository $Repository -WarningAction $WarningPreference
}