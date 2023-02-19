Import-Module $PSScriptRoot\..\ci.psm1 -Scope Local
Import-Module $PSScriptRoot\..\Appveyor.psm1 -DisableNameChecking -Scope Local

$skipBuildModule = $true
. $PSScriptRoot\..\..\..\src\Profiler.Tests\Support\PowerShell\BuildCore.ps1

#region Support

function ReplaceConfig($script)
{
    $file = Get-AppveyorLocalConfigPath

    $existed = $false

    if(Test-Path $file)
    {
        $existed = $true
        $suffix = GetSuffix $file
        Rename-Item $file "$($file)$suffix" -Force
    }

    try
    {
        & $script
    }
    finally
    {
        if($existed)
        {
            if(Test-Path $file)
            {
                Remove-Item $file -Force # Remove the config that was created by this test
            }

            Rename-Item "$($file)$suffix" $file -Force
        }
    }
}

function GetSuffix($file)
{
    $suffix = "_bak"

    if(!(Test-Path "$($file)_bak"))
    {
        return $suffix
    }

    $i = 0

    while($true)
    {
        $suffix = "_bak$i"

        if(!(Test-Path "$($file)$suffix"))
        {
            return $suffix
        }

        $i++
    }
}

function global:Mock-Version
{
    [CmdletBinding()]
    param(
        [ValidateNotNull()]
        $Assembly,

        $LastBuild,

        $LastRelease
    )

    Mock Get-CIVersion {
        [PSCustomObject]@{
            File = [Version]$Assembly
        }
    }.GetNewClosure()

    Mock Get-LastAppveyorBuild {
        $LastBuild
    }.GetNewClosure()

    Mock Get-LastAppveyorNuGetVersion {
        $LastRelease
    }.GetNewClosure()
}

function Simulate-Build
{
    [CmdletBinding()]
    param(
        [ValidateNotNull()]
        [Parameter(Mandatory = $true)]
        $Assembly,

        [Parameter(Mandatory = $false)]
        $LastBuild,

        [Parameter(Mandatory = $false)]
        $LastRelease,

        [ValidateNotNull()]
        [Parameter(Mandatory = $true)]
        $Expected
    )

    $global:simulateBuildArgs = $PSBoundParameters    

    InModuleScope Appveyor {

        $simulateBuildArgs = $global:simulateBuildArgs

        Simulate-Environment {
            Mock-Version -Assembly $simulateBuildArgs.Assembly -LastBuild $simulateBuildArgs.LastBuild -LastRelease $simulateBuildArgs.LastRelease

            $result = Get-AppveyorVersion

            $result | Should Be $simulateBuildArgs.Expected
        }
    }
}

function TestPackageContents
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true, Position = 1)]
        $Contents,

        [Parameter(Mandatory = $false, Position = 2)]
        $Configuration = "Debug",

        [Parameter(Mandatory = $false)]
        [ValidateSet("CSharp", "PowerShell", "Redist")]
        [string]$Type = "CSharp"
    )

    $global:dbgPackageContents = $Contents

    if($Type -eq "CSharp")
    {
        Mock "New-AppveyorPackage" {

            $config = [PSCustomObject]@{
                Configuration = $env:CONFIGURATION
            }

            Test-CSharpPackageContents $config "C:\FakeExtractFolder"
        } -Verifiable -ModuleName "Appveyor"
    }
    elseif($Type -eq "PowerShell")
    {
        Mock "New-AppveyorPackage" {

            $config = [PSCustomObject]@{
                Configuration = $env:CONFIGURATION
            }

            Test-PowerShellPackageContents $config "C:\FakeExtractFolder"
        } -Verifiable -ModuleName "Appveyor"
    }
    elseif($Type -eq "Redist")
    {
        Mock "New-AppveyorPackage" {

            $config = [PSCustomObject]@{
                Configuration = $env:CONFIGURATION
            }

            Test-RedistributablePackageContents $config "C:\FakeExtractFolder"
        } -Verifiable -ModuleName "Appveyor"
    }
    else
    {
        throw "Don't know how to handle type '$Type'"
    }

    InModuleScope "Appveyor" {

        Mock "Get-ChildItem" {

            param($Path)

            if($Path -eq "C:\FakeExtractFolder")
            {
                foreach($content in $global:dbgPackageContents)
                {
                    $joined = Join-Path $Path $content.Path

                    if($content.Type -eq "File")
                    {
                        [System.IO.FileInfo]$joined | Add-Member PSIsContainer $false -PassThru
                    }
                    elseif($content.Type -eq "Folder")
                    {
                        [System.IO.DirectoryInfo]$joined | Add-Member PSIsContainer $true -PassThru
                    }
                    else
                    {
                        throw "Unknown type specified in content '$content'"
                    }
                }
            }
            else
            {
                throw "Unknown path '$Path' was specified"
            }            
        } -Verifiable
    }

    try
    {
        Simulate-Environment {

            $env:CONFIGURATION = $Configuration

            Invoke-AppveyorBeforeTest
        }
    }
    finally
    {
        $global:dbgPackageContents = $null
    }

    Assert-VerifiableMocks
}

#endregion

Describe "Appveyor" {
    It "simulates Appveyor" {

        WithoutTestDrive {
            Simulate-Appveyor
        }
    }

    Context "Version" {

        It "Release 0.1 -> Commit (p1) = Reset Counter = 0.1.1-preview.1" {

            $buildArgs = @{
                Assembly = "0.1.0"
                LastBuild = "0.1.0"
                LastRelease = "0.1.0"
                Expected = "0.1.1-preview.1"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 -> Commit (p1) -> Commit (p2) = 0.1.1-preview.2" {
            $buildArgs = @{
                Assembly = "0.1.0"
                LastBuild = "0.1.1-preview.1"
                LastRelease = "0.1.0"
                Expected = "0.1.1-preview.2"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 -> Commit (p1) -> Commit (p2) -> Set 0.1.1 = 0.1.1" {

            $buildArgs = @{
                Assembly = "0.1.1"
                LastBuild = "0.1.1-preview.2"
                LastRelease = "0.1.0"
                Expected = "0.1.1"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 -> Commit (p1) -> Commit (p2) -> Set 0.1.1 -> Commit (u1) = Reset Counter + 0.1.1-build.1" {
            $buildArgs = @{
                Assembly = "0.1.1"
                LastBuild = "0.1.1"
                LastRelease = "0.1.0"
                Expected = "0.1.1-build.1"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 -> Commit (p1) -> Commit (p2) -> Set/Release 0.1.1 -> Commit (p1) = Reset Counter + 0.1.2-preview.{build}" {
            $buildArgs = @{
                Assembly = "0.1.1"
                LastBuild = "0.1.1"
                LastRelease = "0.1.1"
                Expected = "0.1.2-preview.1"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 -> Release 0.2 = 0.2" {
            $buildArgs = @{
                Assembly = "0.2.0"
                LastBuild = "0.1.0"
                LastRelease = "0.1.0"
                Expected = "0.2.0"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 (u1) -> Release 0.2 = 0.2" {
            $buildArgs = @{
                Assembly = "0.2.0"
                LastBuild = "0.1.0-build.1"
                LastRelease = "0.1.0-build.1"
                Expected = "0.2.0"
            }

            Simulate-Build @buildArgs
        }

        It "Release 0.1 (u1) -> Commit (p1) = 0.1.1-preview.1" {
            $buildArgs = @{
                Assembly = "0.1.0"
                LastBuild = "0.1.0-build.1"
                LastRelease = "0.1.0-build.1"
                Expected = "0.1.1-preview.1"
            }

            Simulate-Build @buildArgs
        }

        It "First Build" {
            $buildArgs = @{
                Assembly = "0.1.0"
                LastBuild = $null
                LastRelease = $null
                Expected = "0.1.0"
            }

            Simulate-Build @buildArgs
        }

        It "First Release, Second Build" {
            $buildArgs = @{
                Assembly = "0.1.0"
                LastBuild = "0.1.0"
                LastRelease = $null
                Expected = "0.1.0-build.1"
            }

            Simulate-Build @buildArgs
        }
    }
    
    Context "C# NuGet" {

        It "has all files for .NET Framework" {
            TestPackageContents $false @(
                @{Type = "File"; Path = "[Content_Types].xml"}
                @{Type = "File"; Path = "_rels\blah.txt"}
                @{Type = "File"; Path = "lib\net472\DebugTools.dll"}
                @{Type = "File"; Path = "lib\net472\DebugTools.xml"}
                @{Type = "File"; Path = "lib\net472\DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "package\foo\bar.txt"}
                @{Type = "File"; Path = "DebugTools.nuspec"}
                @{Type = "Folder"; Path = "_rels"}
                @{Type = "Folder"; Path = "lib"}
                @{Type = "Folder"; Path = "lib\net472"}
                @{Type = "Folder"; Path = "package"}
                @{Type = "Folder"; Path = "package\foo"}
            )
        }

        It "has no contents" {

            $missing = @(
                "'[Content_Types].xml'"
                "'_rels\*'"
                "'lib\net472\DebugTools.dll'"
                "'lib\net472\DebugTools.xml'"
                "'lib\net472\DebugTools.RemoteEndpoint.dll'"
                "'package\*'"
                "'DebugTools.nuspec'"
            )

            { TestPackageContents $false @() } | Should Throw "Package is missing required items:`n$($missing -join "`n")"
        }

        It "is missing one file" {
            { TestPackageContents $false @(
                @{Type = "File"; Path = "_rels\blah.txt"}
                @{Type = "File"; Path = "lib\net472\DebugTools.dll"}
                @{Type = "File"; Path = "lib\net472\DebugTools.xml"}
                @{Type = "File"; Path = "lib\net472\DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "package\foo\bar.txt"}
                @{Type = "File"; Path = "DebugTools.nuspec"}
                @{Type = "Folder"; Path = "_rels"}
                @{Type = "Folder"; Path = "lib"}
                @{Type = "Folder"; Path = "lib\net472"}
                @{Type = "Folder"; Path = "package"}
                @{Type = "Folder"; Path = "package\foo"}
            ) } | Should Throw "Package is missing required items:`n'[Content_Types].xml'"
        }

        It "is missing a wildcard folder" {
            { TestPackageContents $false @(
                @{Type = "File"; Path = "[Content_Types].xml"}
                @{Type = "File"; Path = "_rels\blah.txt"}
                @{Type = "File"; Path = "lib\net472\DebugTools.dll"}
                @{Type = "File"; Path = "lib\net472\DebugTools.xml"}
                @{Type = "File"; Path = "lib\net472\DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "DebugTools.nuspec"}
                @{Type = "Folder"; Path = "_rels"}
                @{Type = "Folder"; Path = "lib"}
                @{Type = "Folder"; Path = "lib\net472"}
            ) } | Should Throw "Package is missing required items:`n'package\*'"
        }
    }

    Context "PowerShell NuGet" {

        It "has all files for .NET Framework" {
            TestPackageContents $false @(
                @{Type = "File"; Path = "[Content_Types].xml"}
                @{Type = "File"; Path = "_rels\blah.txt"}
                @{Type = "File"; Path = "about_DebugTools.help.txt"}
                @{Type = "File"; Path = "DebugTools.dll"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll"}
                @{Type = "File"; Path = "DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "package\foo\bar.txt"}
                @{Type = "File"; Path = "DebugTools.Format.ps1xml"}
                @{Type = "File"; Path = "DebugTools.nuspec"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll-Help.xml"}
                @{Type = "File"; Path = "DebugTools.psd1"}
                @{Type = "File"; Path = "DebugTools.psm1"}
                @{Type = "Folder"; Path = "_rels"}
                @{Type = "Folder"; Path = "Functions"}
                @{Type = "Folder"; Path = "package"}
                @{Type = "Folder"; Path = "package\foo"}
            ) -Type PowerShell
        }

        It "has no contents" {

            $missing = @(
                "'[Content_Types].xml'"
                "'_rels\*'"
                "'about_DebugTools.help.txt'"
                "'package\*'"
                "'DebugTools.dll'"
                "'DebugTools.Format.ps1xml'"
                "'DebugTools.nuspec'"
                "'DebugTools.PowerShell.dll'"
                "'DebugTools.PowerShell.dll-Help.xml'"
                "'DebugTools.RemoteEndpoint.dll'"
                "'DebugTools.psd1'"
                "'DebugTools.psm1'"
                "'DebugTools.Types.ps1xml'"
            )

            { TestPackageContents $false @() -Type PowerShell } | Should Throw "Package is missing required items:`n$($missing -join "`n")"
        }

        It "is missing one file" {
            { TestPackageContents $false @(
                @{Type = "File"; Path = "_rels\blah.txt"}
                @{Type = "File"; Path = "about_DebugTools.help.txt"}
                @{Type = "File"; Path = "DebugTools.dll"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll"}
                @{Type = "File"; Path = "DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "package\foo\bar.txt"}
                @{Type = "File"; Path = "DebugTools.Format.ps1xml"}
                @{Type = "File"; Path = "DebugTools.nuspec"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll-Help.xml"}
                @{Type = "File"; Path = "DebugTools.psd1"}
                @{Type = "File"; Path = "DebugTools.psm1"}
                @{Type = "Folder"; Path = "_rels"}
                @{Type = "Folder"; Path = "Functions"}
                @{Type = "Folder"; Path = "package"}
                @{Type = "Folder"; Path = "package\foo"}
            ) -Type PowerShell } | Should Throw "Package is missing required items:`n'[Content_Types].xml'"
        }

        It "is missing a wildcard folder" {
            { TestPackageContents $false @(
                @{Type = "File"; Path = "[Content_Types].xml"}
                @{Type = "File"; Path = "_rels\blah.txt"}
                @{Type = "File"; Path = "about_DebugTools.help.txt"}
                @{Type = "File"; Path = "DebugTools.dll"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll"}
                @{Type = "File"; Path = "DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "DebugTools.Format.ps1xml"}
                @{Type = "File"; Path = "DebugTools.nuspec"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll-Help.xml"}
                @{Type = "File"; Path = "DebugTools.psd1"}
                @{Type = "File"; Path = "DebugTools.psm1"}   
                @{Type = "Folder"; Path = "_rels"}
                @{Type = "Folder"; Path = "Functions"}
            ) -Type PowerShell } | Should Throw "Package is missing required items:`n'package\*'"
        }
    }

    Context "Redistributable" {
        It "has all files for .NET Framework for Debug" {
            TestPackageContents $false @(
                @{Type = "File"; Path = "about_DebugTools.help.txt"}
                @{Type = "File"; Path = "DebugTools.cmd"}
                @{Type = "File"; Path = "DebugTools.dll"}
                @{Type = "File"; Path = "DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "DebugTools.Format.ps1xml"}
                @{Type = "File"; Path = "DebugTools.pdb"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll-Help.xml"}
                @{Type = "File"; Path = "DebugTools.PowerShell.pdb"}
                @{Type = "File"; Path = "DebugTools.PowerShell.xml"}
                @{Type = "File"; Path = "DebugTools.psd1"}
                @{Type = "File"; Path = "DebugTools.psm1"}
                @{Type = "File"; Path = "DebugTools.xml"}
                @{Type = "Folder"; Path = "Functions"}
            ) -Type Redist
        }

        It "has all files for .NET Framework for Release" {
            TestPackageContents $false @(
                @{Type = "File"; Path = "about_DebugTools.help.txt"}
                @{Type = "File"; Path = "DebugTools.cmd"}
                @{Type = "File"; Path = "DebugTools.dll"}
                @{Type = "File"; Path = "DebugTools.RemoteEndpoint.dll"}
                @{Type = "File"; Path = "DebugTools.Format.ps1xml"}
                @{Type = "File"; Path = "DebugTools.pdb"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll"}
                @{Type = "File"; Path = "DebugTools.PowerShell.dll-Help.xml"}
                @{Type = "File"; Path = "DebugTools.PowerShell.pdb"}
                @{Type = "File"; Path = "DebugTools.PowerShell.xml"}
                @{Type = "File"; Path = "DebugTools.psd1"}
                @{Type = "File"; Path = "DebugTools.psm1"}
                @{Type = "File"; Path = "DebugTools.xml"}
                @{Type = "Folder"; Path = "Functions"}
            ) "Release" -Type Redist
        }
    }
}