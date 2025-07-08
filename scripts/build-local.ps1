#!/usr/bin/env pwsh
#Requires -Version 5.1

<#
.SYNOPSIS
    Local build script for Bank System Microservices that mirrors Azure DevOps pipeline behavior.

.DESCRIPTION
    This script provides a comprehensive local build and test environment that matches the 
    Azure DevOps CI pipeline. It includes options for building with warnings, running tests 
    with code coverage, and generating HTML coverage reports.

.PARAMETER Configuration
    Build configuration. Default is 'Release'.

.PARAMETER SkipTests
    Skip running tests.

.PARAMETER SkipCoverage
    Skip code coverage collection.

.PARAMETER SkipReport
    Skip HTML coverage report generation.

.PARAMETER ShowWarnings
    Show build warnings (equivalent to --verbosity normal).

.PARAMETER CleanFirst
    Clean the solution before building.

.PARAMETER OutputPath
    Custom output path for test results and coverage reports.

.EXAMPLE
    .\build-local.ps1
    Runs full build, test, and coverage pipeline

.EXAMPLE
    .\build-local.ps1 -ShowWarnings -CleanFirst
    Clean build with warnings displayed

.EXAMPLE
    .\build-local.ps1 -SkipTests
    Build only, skip tests

.EXAMPLE
    .\build-local.ps1 -Configuration Debug
    Build in Debug configuration

.NOTES
    - Requires .NET 9 SDK
    - Automatically installs ReportGenerator if not present
    - Mirrors Azure DevOps pipeline steps exactly
    - Supports all microservices in the solution
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [switch]$SkipTests,
    [switch]$SkipCoverage,
    [switch]$SkipReport,
    [switch]$ShowWarnings,
    [switch]$CleanFirst,
    [string]$OutputPath = "./TestResults"
)

# Script configuration
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'
$WarningPreference = 'Continue'

# Constants
$SolutionPath = "src/BankSystem.sln"
$CoverletSettings = "src/coverlet.runsettings"
$CoverageReportPath = "./CoverageReport"

# Colors for output
$Colors = @{
    Success = 'Green'
    Warning = 'Yellow'
    Error = 'Red'
    Info = 'Cyan'
    Header = 'Magenta'
}

# Function to format warnings like Azure DevOps
function Format-AzureDevOpsOutput {
    param(
        [Parameter(ValueFromPipeline)]
        [string]$InputLine
    )
    
    process {
        if ($InputLine -match '.*\.cs\(\d+,\d+\):\s+warning\s+(.*)') {
            # Extract file path and warning details
            if ($InputLine -match '(.+\.cs)\((\d+),(\d+)\):\s+warning\s+(.+)') {
                $filePath = $matches[1]
                $line = $matches[2]
                $column = $matches[3]
                $warning = $matches[4]
                Write-Host "##[warning]$filePath($line,$column): Warning $warning" -ForegroundColor $Colors.Warning
            } else {
                Write-Host $InputLine
            }
        }
        elseif ($InputLine -match '\s*warning\s+(.*)') {
            # General warning format
            Write-Host "##[warning]$($matches[1])" -ForegroundColor $Colors.Warning
        }
        else {
            Write-Host $InputLine
        }
    }
}

function Write-Header {
    param([string]$Message)
    Write-Host "`n$('=' * 80)" -ForegroundColor $Colors.Header
    Write-Host " $Message" -ForegroundColor $Colors.Header
    Write-Host "$('=' * 80)" -ForegroundColor $Colors.Header
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n▶ $Message" -ForegroundColor $Colors.Info
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor $Colors.Success
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor $Colors.Warning
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor $Colors.Error
}

function Test-Prerequisites {
    Write-Step "Checking prerequisites..."
    
    # Check .NET 9 SDK
    try {
        $dotnetVersion = dotnet --version
        if ($dotnetVersion -notmatch '^9\.') {
            Write-Warning ".NET 9 SDK not detected. Current version: $dotnetVersion"
            Write-Host "Install .NET 9 SDK: winget install Microsoft.DotNet.SDK.9" -ForegroundColor $Colors.Info
        } else {
            Write-Success ".NET 9 SDK detected: $dotnetVersion"
        }
    }
    catch {
        Write-Error ".NET SDK not found. Please install .NET 9 SDK."
        exit 1
    }
    
    # Check solution file
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        exit 1
    }
    Write-Success "Solution file found: $SolutionPath"
    
    # Check coverlet settings
    if (-not (Test-Path $CoverletSettings)) {
        Write-Warning "Coverlet settings not found: $CoverletSettings"
        Write-Host "Code coverage may not work as expected." -ForegroundColor $Colors.Warning
    } else {
        Write-Success "Coverlet settings found: $CoverletSettings"
    }
}

function Get-TestProjects {
    Write-Step "Discovering test projects..."
    
    $testProjects = Get-ChildItem -Path "src" -Recurse -Filter "*.csproj" | 
        Where-Object { $_.Name -match "(UnitTests|IntegrationTests)" } |
        ForEach-Object { $_.FullName }
    
    if ($testProjects.Count -eq 0) {
        Write-Warning "No test projects found matching pattern *UnitTests* or *IntegrationTests*"
        return @()
    }
    
    Write-Success "Found $($testProjects.Count) test project(s):"
    $testProjects | ForEach-Object { 
        $relativePath = Resolve-Path -Path $_ -Relative
        Write-Host "  • $relativePath" -ForegroundColor $Colors.Info
    }
    
    return $testProjects
}

function Invoke-CleanSolution {
    Write-Step "Cleaning solution..."
    
    try {
        dotnet clean $SolutionPath --configuration $Configuration --verbosity minimal
        Write-Success "Solution cleaned successfully"
    }
    catch {
        Write-Error "Failed to clean solution: $_"
        exit 1
    }
}

function Invoke-RestorePackages {
    Write-Step "Restoring NuGet packages..."
    
    try {
        dotnet restore $SolutionPath --verbosity minimal
        Write-Success "NuGet packages restored successfully"
    }
    catch {
        Write-Error "Failed to restore packages: $_"
        exit 1
    }
}

function Invoke-BuildSolution {
    Write-Step "Building solution in $Configuration configuration..."
    
    $buildArgs = @(
        'build'
        $SolutionPath
        '--configuration', $Configuration
        '--no-restore'
    )
    
    if ($ShowWarnings) {
        $buildArgs += '--verbosity', 'normal'
        Write-Host "Building with warnings enabled (Azure DevOps format)..." -ForegroundColor $Colors.Info
    } else {
        $buildArgs += '--verbosity', 'minimal'
    }
    
    try {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        
        if ($ShowWarnings) {
            # Capture output and format warnings
            $buildOutput = & dotnet @buildArgs 2>&1
            $buildOutput | Format-AzureDevOpsOutput
        } else {
            & dotnet @buildArgs
        }
        
        $sw.Stop()
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Solution built successfully in $($sw.Elapsed.TotalSeconds.ToString('F1'))s"
        } else {
            Write-Error "Build failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
    }
    catch {
        Write-Error "Failed to build solution: $_"
        exit 1
    }
}

function Invoke-RunTests {
    param([string[]]$TestProjects)
    
    if ($TestProjects.Count -eq 0) {
        Write-Warning "No test projects found to execute"
        return
    }
    
    Write-Step "Running tests with code coverage..."
    
    # Ensure output directory exists
    if (-not (Test-Path $OutputPath)) {
        New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    }
    
    $totalTests = 0
    $totalPassed = 0
    $totalFailed = 0
    $totalTime = 0
    
    foreach ($testProject in $TestProjects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject)
        Write-Host "`n📋 Running tests for: $projectName" -ForegroundColor $Colors.Info
        
        $testArgs = @(
            'test'
            $testProject
            '--configuration', $Configuration
            '--no-build'
            '--results-directory', $OutputPath
            '--logger', 'trx'
        )
        
        if (-not $SkipCoverage) {
            $testArgs += @(
                '--collect:"XPlat Code Coverage"'
                '--settings', $CoverletSettings
            )
        }
        
        try {
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            $output = & dotnet @testArgs 2>&1
            $sw.Stop()
            
            # Parse test results from output
            $testSummary = $output | Where-Object { $_ -match "Test summary:" } | Select-Object -Last 1
            if ($testSummary) {
                Write-Host "  $testSummary" -ForegroundColor $Colors.Info
                
                # Extract numbers for totals
                if ($testSummary -match "total: (\d+).*failed: (\d+).*succeeded: (\d+)") {
                    $totalTests += [int]$Matches[1]
                    $totalFailed += [int]$Matches[2]
                    $totalPassed += [int]$Matches[3]
                }
            }
            
            $totalTime += $sw.Elapsed.TotalSeconds
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Tests completed for $projectName in $($sw.Elapsed.TotalSeconds.ToString('F1'))s"
            } else {
                Write-Error "Tests failed for $projectName with exit code $LASTEXITCODE"
                Write-Host $output -ForegroundColor $Colors.Error
                exit $LASTEXITCODE
            }
        }
        catch {
            Write-Error "Failed to run tests for ${projectName}: $_"
            exit 1
        }
    }
    
    # Display overall test summary
    Write-Header "Test Summary"
    Write-Host "Total Tests: $totalTests" -ForegroundColor $Colors.Info
    Write-Host "Passed: $totalPassed" -ForegroundColor $Colors.Success
    Write-Host "Failed: $totalFailed" -ForegroundColor $(if ($totalFailed -gt 0) { $Colors.Error } else { $Colors.Success })
    Write-Host "Total Time: $($totalTime.ToString('F1'))s" -ForegroundColor $Colors.Info
    
    if ($totalFailed -gt 0) {
        Write-Error "Some tests failed. Check the output above for details."
        exit 1
    } else {
        Write-Success "All tests passed!"
    }
}

function Invoke-GenerateCoverageReport {
    if ($SkipCoverage) {
        Write-Warning "Skipping coverage report generation (coverage collection was skipped)"
        return
    }
    
    Write-Step "Generating HTML coverage report..."
    
    # Check if ReportGenerator is installed
    try {
        $null = Get-Command reportgenerator -ErrorAction Stop
    }
    catch {
        Write-Host "ReportGenerator not found. Installing..." -ForegroundColor $Colors.Info
        try {
            dotnet tool install --global dotnet-reportgenerator-globaltool
            Write-Success "ReportGenerator installed successfully"
        }
        catch {
            Write-Error "Failed to install ReportGenerator: $_"
            return
        }
    }
    
    # Find coverage files
    $coverageFiles = Get-ChildItem -Path $OutputPath -Recurse -Filter "coverage.cobertura.xml"
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found in $OutputPath"
        Write-Host "Coverage files should match pattern: **/coverage.cobertura.xml" -ForegroundColor $Colors.Info
        return
    }
    
    Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor $Colors.Info
    
    # Generate report
    $reportArgs = @(
        '-reports:' + ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'
        "-targetdir:$CoverageReportPath"
        '-reporttypes:Html'
    )
    
    try {
        & reportgenerator @reportArgs
        
        $indexPath = Join-Path $CoverageReportPath "index.html"
        if (Test-Path $indexPath) {
            Write-Success "Coverage report generated: $indexPath"
            
            if (-not $SkipReport) {
                Write-Host "Opening coverage report in browser..." -ForegroundColor $Colors.Info
                Start-Process $indexPath
            }
        } else {
            Write-Error "Coverage report generation failed - index.html not found"
        }
    }
    catch {
        Write-Error "Failed to generate coverage report: $_"
    }
}

function Show-Summary {
    param([System.Diagnostics.Stopwatch]$TotalTime)
    
    Write-Header "Build Summary"
    Write-Host "Configuration: $Configuration" -ForegroundColor $Colors.Info
    Write-Host "Total Time: $($TotalTime.Elapsed.TotalSeconds.ToString('F1'))s" -ForegroundColor $Colors.Info
    
    if (Test-Path $CoverageReportPath) {
        $reportPath = Join-Path $CoverageReportPath "index.html"
        Write-Host "Coverage Report: $reportPath" -ForegroundColor $Colors.Info
    }
    
    if (Test-Path $OutputPath) {
        $trxFiles = Get-ChildItem -Path $OutputPath -Recurse -Filter "*.trx"
        Write-Host "Test Results: $($trxFiles.Count) TRX file(s) in $OutputPath" -ForegroundColor $Colors.Info
    }
    
    Write-Success "Build pipeline completed successfully!"
}

# Main execution
try {
    Write-Header "Bank System Microservices - Local Build Pipeline"
    Write-Host "Configuration: $Configuration" -ForegroundColor $Colors.Info
    Write-Host "Output Path: $OutputPath" -ForegroundColor $Colors.Info
    Write-Host "Skip Tests: $SkipTests" -ForegroundColor $Colors.Info
    Write-Host "Skip Coverage: $SkipCoverage" -ForegroundColor $Colors.Info
    Write-Host "Show Warnings: $ShowWarnings" -ForegroundColor $Colors.Info
    
    $totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    # Step 1: Prerequisites
    Test-Prerequisites
    
    # Step 2: Clean (if requested)
    if ($CleanFirst) {
        Invoke-CleanSolution
    }
    
    # Step 3: Restore
    Invoke-RestorePackages
    
    # Step 4: Build
    Invoke-BuildSolution
    
    # Step 5: Test (if not skipped)
    if (-not $SkipTests) {
        $testProjects = Get-TestProjects
        Invoke-RunTests -TestProjects $testProjects
        
        # Step 6: Coverage Report (if not skipped)
        if (-not $SkipReport) {
            Invoke-GenerateCoverageReport
        }
    }
    
    $totalStopwatch.Stop()
    Show-Summary -TotalTime $totalStopwatch
}
catch {
    Write-Error "Build pipeline failed: $_"
    exit 1
}
