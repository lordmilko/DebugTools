function Get-CodeCoverage
{
    [CmdletBinding()]
    param(
        [string]$Name = "*",
        [string]$BuildFolder = $env:APPVEYOR_BUILD_FOLDER,
        [string[]]$Type,
        [string]$Configuration = "Debug",
        [switch]$TestOnly
    )

    $coverage = [CodeCoverage]::new($Name, $BuildFolder, $Type, $Configuration, $TestOnly)

    $coverage.GetCoverage()
}

class CodeCoverage
{
    static [string]$OpenCover
    static [string]$Temp
    static [string]$PowerShellAdapter
    static [string]$OpenCoverOutput
    static [string]$PowerShellAdapterDownload

    [string]$Name
    [string]$BuildFolder
    [string[]]$Type
    [string]$Configuration
    [switch]$TestOnly

    static CodeCoverage()
    {
        Install-CIDependency OpenCover

        [CodeCoverage]::OpenCover = Get-ChocolateyCommand "OpenCover.Console.exe"
        [CodeCoverage]::Temp = [IO.Path]::GetTempPath()
        [CodeCoverage]::OpenCoverOutput = Join-Path ([CodeCoverage]::Temp) "opencover.xml"
    }

    CodeCoverage($name, $buildFolder, $type, $configuration, $testOnly)
    {
        $this.Name = $name
        $this.BuildFolder = $buildFolder
        $this.Type = $type
        $this.Configuration = $configuration
        $this.TestOnly = $testOnly
    }

    [void]GetCoverage()
    {
        $this.ClearCoverage()

        $this.GetPowerShellCoverage()
        $this.GetCSharpCoverage()
    }

    #region C#

    [void]GetCSharpCoverage()
    {
        if(!($this.Type | HasType "C#"))
        {
            return
        }

        $testRunner = $this.GetCSharpTestRunner()
        $testParams = $this.GetCSharpTestParams()

        if($this.TestOnly)
        {
            Write-LogInfo "`t`tExecuting $testRunner $testParams"
            Invoke-Process { & $testRunner @testParams } -WriteHost
        }
        else
        {
            $opencoverParams = $this.GetCSharpOpenCoverParams($testRunner, $testParams)

            Write-LogInfo "`t`tExecuting '$([CodeCoverage]::OpenCover) $opencoverParams'"
            Invoke-Process { & ([CodeCoverage]::OpenCover) @opencoverParams } -WriteHost
        }
    }

    [string]GetCSharpTestRunner()
    {
        return Get-VSTest
    }

    [object]GetCSharpTestParams()
    {
        $nameFilter = $null

        $trimmedName = $this.name.Trim('*')

        if(![string]::IsNullOrEmpty($trimmedName))
        {
            $nameFilter = "&FullyQualifiedName~$trimmedName"
        }

        $filter = "TestCategory!=SkipCoverage&TestCategory!=SkipCI$nameFilter"

        $testParams = @()

        $dll = Join-PathEx $this.BuildFolder artifacts bin $($this.Configuration) Profiler.Tests.dll

        $testParams = @(
            "/TestCaseFilter:$filter"
            "\`"$dll\`""
        )

        if($this.TestOnly)
        {
            # Replace the cmd escaped quotes with PowerShell escaped quotes, and then add an additional quote at the end of the TestCaseFilter to separate the arguments.
            # Trim any quotes from the end of the string, since PowerShell will add its own quote for us
            $testParams = $testParams | foreach {
                ($_ -replace "\\`"","`" `"").Trim("`" `"")
            }
        }

        return $testParams
    }

    [object]GetCSharpOpenCoverParams($testRunner, $testParams)
    {
        $opencoverParams = $this.GetCommonOpenCoverParams($testRunner, $testParams)

        $opencoverParams += "-mergeoutput"

        return $opencoverParams
    }

    #endregion
    #region PowerShell

    [void]GetPowerShellCoverage()
    {
        if(!($this.Type | HasType "PowerShell"))
        {
            return
        }

        $tests = $this.GetPowerShellTests()

        if($tests.Count -eq 0)
        {
            Write-LogInfo "`t`tNo tests matched the specified name '$($this.Name)'; skipping PowerShell coverage"
            return
        }

        $this.AssertHasPowerShellDll()

        $testRunner = $this.GetPowerShellTestRunner()
        $testParams = $this.GetPowerShellTestParams($tests)

        if($this.TestOnly)
        {
            Write-LogInfo "`t`tExecuting $testRunner $testParams"
            Invoke-Process { & $testRunner @testParams } -WriteHost
        }
        else
        {
            $opencoverParams = $this.GetPowerShellOpenCoverParams($testRunner, $testParams)

            Write-LogInfo "`t`tExecuting $([CodeCoverage]::OpenCover) $opencoverParams"
            Invoke-Process { & ([CodeCoverage]::OpenCover) @opencoverParams } -WriteHost
        }
    }

    [void]AssertHasPowerShellDll()
    {
        $binPath = (Resolve-Path $PSScriptRoot\..\..\..\src\Profiler.Tests\bin).Path

        $directory = gci $binPath |sort lastwritetime -Descending|select -first 1 -expand FullName

        $dll = Join-Path $directory "Profiler.Tests.dll"

        if(!(Test-Path $dll))
        {
            throw "DebugTools for PowerShell Desktop is required to run PowerShell tests however '$dll' is missing. Has DebugTools been compiled?"
        }
    }

    [string]GetPowerShellTestRunner()
    {
        return Get-VSTest
    }

    [object]GetPowerShellTestParams($tests)
    {
        $this.InstallPowerShellAdapter()

        $testsStr = $tests -join " "
        $vstestParams = $null

        $testAdapterPath = Join-PathEx $this.BuildFolder src Tools PowerShell.TestAdapter bin Release netstandard2.0

        $testParams = "/TestAdapterPath:\`"$testAdapterPath\`""

        if($this.TestOnly)
        {
            return @(
                $tests | foreach { $_ -replace "\\`"","`"" }
                $testParams -replace "\\`"","`""
            )
        }
        else
        {
            $testParams = "$testsStr $testParams"
        }

        return $testParams
    }

    [object]GetPowerShellTests()
    {
        $testRoot = Join-Path $this.BuildFolder "src\Profiler.Tests\PowerShell"

        $tests = gci $testRoot -Recurse -Filter *.Tests.ps1 | where {
            # Blacklist Solution.Tests.ps1 as this invokes Invoke-DbgAnalyzer which causes vstest.console to hang on completion
            # Also blacklist build tests as these do not pertain to the DebugTools assemblies
            $_.BaseName -like $this.Name -and $_.DirectoryName -notlike "*Build*" -and $_.BaseName -ne "Solution.Tests"
        } | foreach {"\`"$($_.FullName)\`""}

        if($tests -eq $null -or $tests.Count -eq 0)
        {
            if($tests -eq $null)
            {
                $tests = @()
            }

            if($this.Name -eq "*")
            {
                throw "Couldn't find any PowerShell tests"
            }
        }
        else
        {
            Write-LogInfo "`t`tFound $($tests.Count) PowerShell tests"
        }

        return $tests
    }

    [object]GetPowerShellOpenCoverParams($testRunner, $testParams)
    {
        return $this.GetCommonOpenCoverParams($testRunner, $testParams)
    }

    [void]InstallPowerShellAdapter()
    {
        $adapterPath = Join-Path $this.BuildFolder "src\Tools\PowerShell.TestAdapter\bin\Release\netstandard2.0\PowerShell.TestAdapter.dll"

        if(!(Test-Path $adapterPath))
        {
            $csproj = Join-Path $this.BuildFolder "src\Tools\PowerShell.TestAdapter\PowerShell.TestAdapter.csproj"

            Write-Host $csproj

            Install-CIDependency dotnet

            Invoke-Process {
               dotnet build $csproj -c Release
            } -WriteHost
        }
    }

    #endregion
    #region OpenCover

    [void]ClearCoverage()
    {
        if(Test-Path ([CodeCoverage]::OpenCoverOutput))
        {
            Remove-Item ([CodeCoverage]::OpenCoverOutput) -Force
        }
    }

    [object]GetCommonOpenCoverParams($testRunner, $testParams)
    {
        $opencoverParams = @(
            "-target:$testRunner"
            "-targetargs:$testParams"
            "-output:`"$($([CodeCoverage]::OpenCoverOutput))`""
            "-filter:+`"[DebugTools*]* -[Profiler.Tests*]*`""
            "-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute"
            "-hideskipped:attribute"
            "-register:path32"
        )

        return $opencoverParams
    }

    #endregion
}

function New-CoverageReport
{
    [CmdletBinding()]
    param(
        [string]$Types = "Html",
        [string]$TargetDir = (Join-Path ([IO.Path]::GetTempPath()) "report")
    )

    Write-LogHeader "Generating a coverage report"

    Install-CIDependency ReportGenerator

    $reportParams = @(
        "-reports:$([CodeCoverage]::OpenCoverOutput)"
        "-reporttypes:$Types"
        "-targetdir:$TargetDir"
        "-verbosity:off"
    )

    $reportgenerator = Get-ReportGeneratorCommand

    Write-LogInfo "`t`tExecuting '$reportgenerator $reportParams'"
    Invoke-Process { & $reportgenerator @reportParams }
}

function Get-ReportGeneratorCommand
{
    $reportgenerator = Get-ChocolateyCommand "reportgenerator" -AllowPath:$false

    if(([Version](gi $reportgenerator).VersionInfo.FileVersion) -ge [Version]"4.3")
    {
        # The chocolatey shim probably points to the .NET Core 3.0 version. Fallback
        # to the .NET Framework version

        $bin = Split-Path $reportgenerator -Parent
        $chocolatey = Split-Path $bin -Parent

        $tools = "$chocolatey\lib\reportgenerator.portable\tools"

        if(!(Test-Path $tools))
        {
            throw "Folder '$tools' does not exist. Unable to locate .NET Framework Report Generator"
        }

        $netfx = gci $tools net4*|select -First 1

        if(!$netfx)
        {
            throw "Unable to find a .NET Framework version of Report Generator"
        }

        return ($tools + "\$($netfx.Name)\reportgenerator.exe") -replace "/","\"
    }

    return $reportgenerator
}

function Get-LineCoverage
{
    New-CoverageReport CsvSummary

    $csv = Import-Csv $env:temp\report\Summary.csv -Delimiter ';' -Header "Property","Value"

    $val = ($csv|where property -eq "Line coverage:"|select -expand value).Trim("%")

    return [double]$val
}

Export-ModuleMember New-CoverageReport,Get-LineCoverage