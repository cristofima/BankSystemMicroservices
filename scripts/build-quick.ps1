#!/usr/bin/env pwsh
#Requires -Version 5.1

<#
.SYNOPSIS
    Quick build script for Bank System Microservices.

.DESCRIPTION
    Simple build script that performs the most common development tasks:
    - Clean and build solution with warnings
    - Run all tests with coverage
    - Generate and open coverage report

.PARAMETER Configuration
    Build configuration. Default is 'Release'.

.PARAMETER TestsOnly
    Skip build and run tests only.

.PARAMETER BuildOnly
    Build only, skip tests.

.EXAMPLE
    .\build-quick.ps1
    Full build, test, and coverage

.EXAMPLE
    .\build-quick.ps1 -Configuration Debug
    Debug build with tests

.EXAMPLE
    .\build-quick.ps1 -TestsOnly
    Run tests only

.EXAMPLE
    .\build-quick.ps1 -BuildOnly
    Build only, no tests
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    
    [switch]$TestsOnly,
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

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
                Write-Host "##[warning]$filePath($line,$column): Warning $warning" -ForegroundColor Yellow
            } else {
                Write-Host $InputLine
            }
        }
        elseif ($InputLine -match '\s*warning\s+(.*)') {
            # General warning format
            Write-Host "##[warning]$($matches[1])" -ForegroundColor Yellow
        }
        else {
            Write-Host $InputLine
        }
    }
}

Write-Host "`nBank System Microservices - Quick Build" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Warnings will be formatted in Azure DevOps style`n" -ForegroundColor Yellow

try {
    if (-not $TestsOnly) {
        Write-Host "Restoring packages..." -ForegroundColor Green
        dotnet restore src/BankSystem.sln --verbosity minimal
        
        Write-Host "Building solution..." -ForegroundColor Green
        $buildOutput = dotnet build src/BankSystem.sln --configuration $Configuration --no-restore --verbosity normal 2>&1
        $buildOutput | Format-AzureDevOpsOutput
        
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE"
        }
    }
    
    if (-not $BuildOnly) {
        Write-Host "Running unit tests..." -ForegroundColor Green
        dotnet test src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj `
            --configuration $Configuration `
            --no-build `
            --collect:"XPlat Code Coverage" `
            --results-directory ./TestResults `
            --logger trx `
            --settings src/coverlet.runsettings
        
        Write-Host "Running integration tests..." -ForegroundColor Green
        dotnet test src/services/Security/tests/Security.Infrastructure.IntegrationTests/Security.Infrastructure.IntegrationTests.csproj `
            --configuration $Configuration `
            --no-build `
            --collect:"XPlat Code Coverage" `
            --results-directory ./TestResults `
            --logger trx `
            --settings src/coverlet.runsettings
        
        Write-Host "Generating coverage report..." -ForegroundColor Green
        
        # Install ReportGenerator if needed
        if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
            Write-Host "Installing ReportGenerator..." -ForegroundColor Yellow
            dotnet tool install --global dotnet-reportgenerator-globaltool
        }
        
        reportgenerator `
            -reports:./TestResults/**/coverage.cobertura.xml `
            -targetdir:./CoverageReport `
            -reporttypes:Html
        
        Write-Host "Opening coverage report..." -ForegroundColor Green
        Start-Process "./CoverageReport/index.html"
    }
    
    Write-Host "`nBuild completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "`nBuild failed: $_" -ForegroundColor Red
    exit 1
}
