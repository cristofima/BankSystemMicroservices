#!/usr/bin/env pwsh

# Run all unit tests across all services
# This script runs unit tests with code coverage collection

Write-Host "Running all unit tests..." -ForegroundColor Green

# Ensure we're in the correct directory
Set-Location $PSScriptRoot\..

# Run all unit tests
dotnet test src/BankSystem.sln `
  --configuration Release `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults `
  --logger trx `
  --settings src/coverlet.runsettings `
  --verbosity normal `
  --filter "FullyQualifiedName~UnitTests"

if ($LASTEXITCODE -eq 0) {
    Write-Host "All unit tests passed!" -ForegroundColor Green
} else {
    Write-Host "Some unit tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}
