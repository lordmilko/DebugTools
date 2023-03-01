$ProgressPreference = "SilentlyContinue"
$ErrorActionPreference = "Stop"

function New-AppveyorPackage
{
    [CmdletBinding()]
    param()

    Write-LogHeader "Building NuGet Package"

    $config = [PSCustomObject]@{
        SolutionRoot          = "$env:APPVEYOR_BUILD_FOLDER"
        CSharpProjectRoot     = "$env:APPVEYOR_BUILD_FOLDER\src\DebugTools"
        CSharpOutputDir       = "$env:APPVEYOR_BUILD_FOLDER\src\DebugTools\bin\$env:CONFIGURATION"
        PowerShellProjectRoot = "$env:APPVEYOR_BUILD_FOLDER\src\DebugTools.PowerShell"
        PowerShellOutputDir   = Get-PowerShellOutputDir $env:APPVEYOR_BUILD_FOLDER $env:CONFIGURATION
        Manager               = New-PackageManager
        Configuration         = $env:CONFIGURATION
    }

    Process-PowerShellPackage $config
}

function Get-CSharpNupkg
{
    $nupkg = @(gci (PackageManager -RepoLocation) -Filter *.nupkg | where {
        $_.Name -NotLike "*.symbols.nupkg" -and $_.Name -notlike "*.snupkg"
    })

    if(!$nupkg)
    {
        throw "Could not find nupkg for project 'DebugTools'"
    }

    if($nupkg.Count -gt 1)
    {
        $str = "Found more than one nupkg for project 'DebugTools': "

        $names = $nupkg|select -ExpandProperty name|foreach { "'$_'" }

        $str += [string]::Join(", ", $names)

        throw $str
    }

    return $nupkg
}

#region PowerShell

function Process-PowerShellPackage($config)
{
    Write-LogSubHeader "`tProcessing PowerShell package"

    $config.Manager.InstallPowerShellRepository()

    if($env:APPVEYOR)
    {
        Update-ModuleManifest "$($config.PowerShellOutputDir)\DebugTools.psd1"
    }

    $powershellArgs = @{
        OutputDir = $config.PowerShellOutputDir
        RepoManager = $config.Manager
        Configuration = $env:CONFIGURATION
        PowerShell = $true
        Redist = $true
    }

    New-PowerShellPackage @powershellArgs

    Test-PowerShellPackage $config

    Test-RedistributablePackage $config

    Move-AppveyorPackages $config "_PowerShell"

    $config.Manager.UninstallPowerShellRepository()
}

function Test-PowerShellPackage
{
    Write-LogInfo "`t`tTesting package"

    $nupkg = Get-CSharpNupkg

    Extract-Package $nupkg {

        param($extractFolder)

        Test-PowerShellPackageDefinition $config $extractFolder
        Test-PowerShellPackageContents $config $extractFolder
    }

    Test-PowerShellPackageInstalls
}

function Test-PowerShellPackageDefinition($config, $extractFolder)
{
    Write-LogInfo "`t`t`tValidating package definition"

    $psd1Path = Join-Path $extractFolder "DebugTools.psd1"

    $psd1 = Import-PowerShellDataFile $psd1Path

    $version = GetVersion

    $expectedUrl = "https://github.com/lordmilko/DebugTools/releases/tag/v$version"

    if(!$psd1.PrivateData.PSData.ReleaseNotes.Contains($expectedUrl))
    {
        throw "Release notes did not contain correct release version. Expected notes to contain URL '$expectedUrl'. Release notes were '$($psd1.PrivateData.PSData.ReleaseNotes)'"
    }

    if($env:APPVEYOR)
    {
        if($psd1.CmdletsToExport -eq "*" -or !($psd1.CmdletsToExport -contains "Start-DbgProfiler"))
        {
            throw "Module manifest was not updated to specify exported cmdlets"
        }
    }
}

function Test-PowerShellPackageContents($config, $extractFolder)
{
    $required = @(
        "package\*"
        "_rels\*"
        "DebugTools.nuspec"
        "DebugTools.Format.ps1xml"
        "DebugTools.psd1"
        "[Content_Types].xml"
    )

    $required += @(
        "ClrDebug.dll"
        "DebugTools.dll"
        "DebugTools.PowerShell.dll"

        "DebugTools.Host.x86.exe"
        "DebugTools.Host.x64.exe"

        "x86\Profiler.x86.dll"
        "x64\Profiler.x64.dll"

        "Dia2Lib.dll"
        "EnvDTE.dll"
        "EnvDTE80.dll"
        "Microsoft.Diagnostics.FastSerialization.dll"
        "Microsoft.Diagnostics.Runtime.dll"
        "Microsoft.Diagnostics.Tracing.TraceEvent.dll"
        "OSExtensions.dll"
        "stdole.dll"
        "System.Runtime.CompilerServices.Unsafe.dll"
        "TraceReloggerLib.dll"
    )

    Test-PackageContents $extractFolder $required
}

function Test-PowerShellPackageInstalls
{
    Write-LogInfo "`t`t`tInstalling Package"

    Test-PowerShellPackageInstallsHidden $PSEdition
}

function Test-PowerShellPackageInstallsHidden($edition, $config)
{
    if([string]::IsNullOrEmpty($edition))
    {
        $edition = "Desktop"
    }

    Write-LogInfo "`t`t`t`tTesting package installs on $edition"

    Hide-Module $edition {

        param($edition)

        if(!(Install-EditionPackage $edition DebugTools -Source (PackageManager -RepoName) -AllowClobber)) # TShell has a Get-Device cmdlet
        {
            throw "DebugTools did not install properly"
        }

        Write-LogInfo "`t`t`t`t`tTesting Package cmdlets"

        try
        {
            Test-PowerShellPackageInstallsInternal $edition
        }
        finally
        {
            Write-LogInfo "`t`t`t`t`tUninstalling Package"

            if(!(Uninstall-EditionPackage $edition DebugTools))
            {
                throw "DebugTools did not uninstall properly"
            }
        }
    }
}

function Test-PowerShellPackageInstallsInternal($edition, $module = "DebugTools")
{
    $exe = Get-PowerShellExecutable $edition

    Write-LogInfo "`t`t`t`t`t`tValidating '$exe' cmdlet output"

    $resultCmdlet =   (& $exe -command "&{ import-module '$module'; try { Get-SOSAppDomain } catch [exception] { `$_.exception.message }}")

    if($resultCmdlet -ne "Cannot execute cmdlet: no -Session was specified and no global Session could be found in the PowerShell session.")
    {
        throw $resultCmdlet
    }
}

function Get-PowerShellExecutable($edition)
{
    return "powershell.exe"
}

function Get-EditionModule
{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Edition
    )

    $command = "Get-Module DebugTools -ListAvailable"

    if($PSEdition -eq $Edition)
    {
        return Invoke-Expression $command | Select Path,Version
    }
    else
    {
        $response = Invoke-Edition $Edition "$command | foreach { `$_.Path + '|' + `$_.Version }"

        foreach($line in $response)
        {
            $split = $line.Split('|')

            [PSCustomObject]@{
                Path = $split[0]
                Version = $split[1]
            }
        }
    }
}

function Install-EditionPackage
{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Edition,

        [Parameter(Mandatory = $true, Position = 1)]
        [string]$Name,

        [Parameter()]
        [string]$Source,

        [Parameter()]
        [switch]$AllowClobber
    )

    $command = "Install-Package $Name -Source $Source -AllowClobber:([bool]'$AllowClobber')"

    if($PSEdition -eq $Edition)
    {
        return Invoke-Expression $command
    }
    else
    {
        $response = Invoke-Edition $Edition "$command | foreach { `$_.Name + '|' + `$_.Version }"

        foreach($line in $response)
        {
            $split = $line.Split('|')

            [PSCustomObject]@{
                Name = $split[0]
                Version = $split[1]
            }
        }
    }
}

function Uninstall-EditionPackage
{
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string]$Edition,

        [Parameter(Mandatory = $true, Position = 1)]
        [string]$Name
    )

    $command = "Uninstall-Package $Name"

    if($PSEdition -eq $Edition)
    {
        return Invoke-Expression $command
    }
    else
    {
        $response = Invoke-Edition $Edition "$command | foreach { `$_.Name + '|' + `$_.Version }"

        foreach($line in $response)
        {
            $split = $line.Split('|')

            [PSCustomObject]@{
                Name = $split[0]
                Version = $split[1]
            }
        }
    }
}

function Invoke-Edition($edition, $command)
{
    if($PSEdition -eq $edition)
    {
        throw "Cannot invoke command '$command' in edition 'edition': edition is the same as the currently running process"
    }
    else
    {
        $exe = Get-PowerShellExecutable $edition

        $response = & $exe -Command $command

        if($LASTEXITCODE -ne 0)
        {
            throw "'$edition' invocation failed with exit code '$LASTEXITCODE': $response"
        }

        return $response
    }
}

function Hide-Module($edition, $script)
{
    $hidden = $false

    $module = Get-EditionModule $edition

    try
    {
        if($module)
        {
            $hidden = $true

            Write-LogInfo "`t`t`t`t`tRenaming module info files"

            foreach($m in $module)
            {
                # Rename the module info file so the package manager doesn't find it even inside
                # the renamed folder

                $moduleInfo = $m.Path -replace "DebugTools.psd1","PSGetModuleInfo.xml"

                if(Test-Path $moduleInfo)
                {
                    Rename-Item $moduleInfo "PSGetModuleInfo_bak.xml"
                }
            }

            Write-LogInfo "`t`t`t`t`tRenaming module directories"

            foreach($m in $module)
            {
                $path = Get-ModuleFolder $m

                # Check if we haven't already renamed the folder as part of a previous module
                if(Test-Path $path)
                {
                    try
                    {
                        Rename-Item $path "DebugTools_bak"
                    }
                    catch
                    {
                        throw "$path could not be renamed to 'DebugTools_bak' properly: $($_.Exception.Message)"
                    }

                    if(Test-Path $path)
                    {
                        throw "$path did not rename properly"
                    }
                }
            }
        }

        Write-LogInfo "`t`t`t`t`tInvoking script"

        & $script $edition
    }
    finally
    {
        if($hidden)
        {
            Write-LogInfo "`t`t`t`t`tRestoring module directories"

            foreach($m in $module)
            {
                $path = (split-path (Get-ModuleFolder $m) -parent) + "\DebugTools_bak"

                # Check if we haven't already renamed the folder as part of a previous module
                if(Test-Path $path)
                {
                    Rename-Item $path "DebugTools"
                }
            }

            Write-LogInfo "`t`t`t`t`tRestoring module info files"

            foreach($m in $module)
            {
                $moduleInfo = $m.Path -replace "DebugTools.psd1","PSGetModuleInfo_bak.xml"

                if(Test-Path $moduleInfo)
                {
                    Rename-Item $moduleInfo "PSGetModuleInfo.xml"
                }
            }
        }
    }
}

function Get-ModuleFolder($module)
{
    $path = $module.Path -replace "DebugTools.psd1",""

    $versionFolder = "$($module.Version)\"

    if($path.EndsWith($versionFolder))
    {
        $path = $path.Substring(0, $path.Length - $versionFolder.Length)
    }

    return $path
}

#endregion
#region Redist

function Test-RedistributablePackage($config)
{
    Write-LogInfo "`t`tProcessing Redistributable package"

    $zipPath = Join-Path (PackageManager -RepoLocation) "DebugTools.zip"

    Extract-Package (gi $zipPath) {

        param($extractFolder)

        $psd1Path = Join-Path $extractFolder "DebugTools.psd1"

        Test-RedistributablePackageContents $config $extractFolder
        Test-RedistributableModuleInstalls $config $extractFolder
    }
}

function Test-RedistributablePackageContents($config, $extractFolder)
{
    $optional = @(
        "ClrDebug.pdb"
        "ClrDebug.xml"
    )

    $required = @(
        "ClrDebug.dll"
        "DebugTools.cmd"
        "DebugTools.Format.ps1xml"
        "DebugTools.psd1"
        "DebugTools.dll"
        "DebugTools.pdb"
        "DebugTools.PowerShell.dll"
        "DebugTools.PowerShell.pdb"

        "DebugTools.Host.x86.exe"
        "DebugTools.Host.x64.exe"
        "DebugTools.Host.x86.pdb"
        "DebugTools.Host.x64.pdb"
        "DebugTools.Host.x86.exe.config"
        "DebugTools.Host.x64.exe.config"

        "x86\Profiler.x86.dll"
        "x64\Profiler.x64.dll"
        "x86\Profiler.x86.pdb"
        "x64\Profiler.x64.pdb"
        "x86\Profiler.x86.lib"
        "x64\Profiler.x64.lib"
        "x86\Profiler.x86.exp"
        "x64\Profiler.x64.exp"

        "Dia2Lib.dll"
        "EnvDTE.dll"
        "EnvDTE80.dll"
        "Microsoft.Diagnostics.FastSerialization.dll"
        "Microsoft.Diagnostics.Runtime.dll"
        "Microsoft.Diagnostics.Tracing.TraceEvent.dll"
        "OSExtensions.dll"
        "stdole.dll"
        "System.Runtime.CompilerServices.Unsafe.dll"
        "TraceReloggerLib.dll"
    )

    Test-PackageContents $extractFolder $required $optional
}

function Test-RedistributableModuleInstalls($config, $extractFolder)
{
    Test-PowerShellPackageInstallsInternal "Desktop" $extractFolder
}

#endregion

function Move-AppveyorPackages($config, $suffix)
{
   if($env:APPVEYOR)
   {
        Write-LogInfo "`t`t`tMoving Appveyor artifacts"
        
        if(!$suffix)
        {
            $suffix = ""
        }

        Move-Packages $suffix $config.SolutionRoot | Out-Null
    }
    else
    {
        Write-LogInfo "`t`t`t`tClearing repo (not running under Appveyor)"
        Clear-Repo
    } 
}

function Clear-Repo
{
    gci -recurse (PackageManager -RepoLocation)|remove-item -Recurse -Force
}

function Extract-Package($package, $script)
{
    $originalExtension = $package.Extension
    $newName = $package.Name -replace $originalExtension,".zip"

    $extractFolder = $package.FullName -replace $package.Extension,""

    $newItem = $null

    try
    {
        $newItem = Rename-Item -Path $package.FullName -NewName $newName -PassThru
        Expand-Archive $newItem.FullName $extractFolder

        & $script $extractFolder
    }
    finally
    {
        Remove-Item $extractFolder -Recurse -Force
        Rename-Item $newItem.FullName $package.Name
    }
}

function Test-PackageContents($folder, $required, $optional = $null)
{
    Write-LogInfo "`t`t`tValidating package contents"

    $pathWithoutTrailingSlash = $folder.TrimEnd("\", "/")

    $existing = gci $folder -Recurse|foreach {
        [PSCustomObject]@{
            Name = $_.fullname.substring($pathWithoutTrailingSlash.length + 1)
            IsFolder = $_.PSIsContainer
        }
    }

    $found = @()
    $illegal = @()

    foreach($item in $existing)
    {
        if($item.IsFolder)
        {
            # Do we have a folder that contains a wildcard that matches this folder? (e.g. packages\* covers packages\foo)
            $match = $required | where { $item.Name -like $_ }

            if(!$match)
            {
                # There isn't a wildcard that covers this folder, but if there are actually any items contained under this folder
                # then transitively this folder is allowed

                $match = $required | where { $_ -like "$($item.Name)\*" }

                # If there is a match, we don't care - we don't whitelist empty folders, so we'll leave it up to the file processing block
                # to decide whether the required files have been found or not
                if(!$match)
                {
                    $illegal += $item.Name
                }
            }
            else
            {
                # Add our wildcard folder (e.g. packages\*)
                $found += $match
            }
        }
        else
        {
            # If there isnt a required item that case insensitively matches a file that appears
            # to exist, then that file must be "extra" and is therefore considered illegal
            $match = $required | where { $_ -eq $item.Name }

            if(!$match)
            {
                # We don't have a direct matchm however maybe we have a folder that contains a wildcard
                # that matches this file (e.g. packages\* covers packages\foo.txt)
                $match = $required | where { $item.Name -like $_ }
            }

            if(!$match)
            {
                $illegal += $item.Name
            }
            else
            {
                $found += $match
            }
        }
    }

    if ($optional)
    {
        $newIllegal = @()

        foreach($item in $illegal)
        {
            $match = $optional | where { $item -like $_ }

            if(!$match)
            {
                $newIllegal += $match
            }
        }

        $illegal = $newIllegal
    }

    if($illegal)
    {
        $str = ($illegal | Sort-Object | foreach { "'$_'" }) -join "`n"
        throw "Package contained illegal items:`n$str"
    }

    $missing = $required | where { $_ -notin $found }

    if($missing)
    {
        $str = ($missing | Sort-Object | foreach { "'$_'" }) -join "`n"
        throw "Package is missing required items:`n$str"
    }
}