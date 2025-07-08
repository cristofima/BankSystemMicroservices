@echo off
setlocal enabledelayedexpansion

rem Bank System Microservices - Local Build Script (Batch)
rem Usage: build-local.bat [Debug|Release] [BuildOnly|TestsOnly]
rem Note: For Azure DevOps-style warning formatting, use build-quick.ps1 or build-local.ps1

set "CONFIGURATION=%~1"
set "MODE=%~2"

if "%CONFIGURATION%"=="" set "CONFIGURATION=Release"
if "%MODE%"=="" set "MODE=Full"

echo.
echo =============================================================================
echo  Bank System Microservices - Local Build Pipeline
echo =============================================================================
echo Configuration: %CONFIGURATION%
echo Mode: %MODE%
echo.

rem Change to repository root (parent of scripts folder)
cd /d "%~dp0.."

rem Check if .NET is installed
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET SDK not found. Please install .NET 9 SDK.
    exit /b 1
)

rem Build phase
if not "%MODE%"=="TestsOnly" (
    echo [1/4] Restoring NuGet packages...
    dotnet restore src/BankSystem.sln --verbosity minimal
    if errorlevel 1 (
        echo ERROR: Package restore failed
        exit /b 1
    )
    
    echo [2/4] Building solution with warnings...
    dotnet build src/BankSystem.sln --configuration %CONFIGURATION% --no-restore --verbosity normal
    if errorlevel 1 (
        echo ERROR: Build failed
        exit /b 1
    )
    echo SUCCESS: Build completed
)

rem Test phase
if not "%MODE%"=="BuildOnly" (
    echo [3/4] Running tests with coverage...
    
    rem Run unit tests
    echo Running unit tests...
    dotnet test "src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj" ^
        --configuration %CONFIGURATION% ^
        --no-build ^
        --collect:"XPlat Code Coverage" ^
        --results-directory ./TestResults ^
        --logger trx ^
        --settings src/coverlet.runsettings
    if errorlevel 1 (
        echo ERROR: Unit tests failed
        exit /b 1
    )
    
    rem Run integration tests
    echo Running integration tests...
    dotnet test "src/services/Security/tests/Security.Infrastructure.IntegrationTests/Security.Infrastructure.IntegrationTests.csproj" ^
        --configuration %CONFIGURATION% ^
        --no-build ^
        --collect:"XPlat Code Coverage" ^
        --results-directory ./TestResults ^
        --logger trx ^
        --settings src/coverlet.runsettings
    if errorlevel 1 (
        echo ERROR: Integration tests failed
        exit /b 1
    )
    
    echo [4/4] Generating coverage report...
    
    rem Check if ReportGenerator is installed
    reportgenerator --help >nul 2>&1
    if errorlevel 1 (
        echo Installing ReportGenerator...
        dotnet tool install --global dotnet-reportgenerator-globaltool
        if errorlevel 1 (
            echo ERROR: Failed to install ReportGenerator
            exit /b 1
        )
    )
    
    rem Generate coverage report
    reportgenerator ^
        -reports:./TestResults/**/coverage.cobertura.xml ^
        -targetdir:./CoverageReport ^
        -reporttypes:Html
    if errorlevel 1 (
        echo ERROR: Coverage report generation failed
        exit /b 1
    )
    
    echo Opening coverage report...
    start "" "./CoverageReport/index.html"
)

echo.
echo =============================================================================
echo  BUILD COMPLETED SUCCESSFULLY
echo =============================================================================
echo Configuration: %CONFIGURATION%
if exist "./CoverageReport/index.html" (
    echo Coverage Report: ./CoverageReport/index.html
)
if exist "./TestResults" (
    echo Test Results: ./TestResults/
)
echo.

endlocal
