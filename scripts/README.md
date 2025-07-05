# Build Scripts

This folder contains PowerShell scripts for building, testing, and generating code coverage reports for the Bank System Microservices project.

## Available Scripts

### `build-quick.ps1` (Recommended)

Fast and focused build script for everyday development.

**Usage:**

```powershell
# Full build, test, and coverage generation
.\scripts\build-quick.ps1

# Build only (no tests)
.\scripts\build-quick.ps1 -BuildOnly

# Tests only (skip build)
.\scripts\build-quick.ps1 -TestsOnly

# Debug configuration
.\scripts\build-quick.ps1 -Configuration Debug
```

**Features:**

- ✅ Fast execution
- ✅ Code coverage collection
- ✅ HTML coverage report generation
- ✅ Automatic browser opening for results
- ✅ Clean output formatting

### `build-local.ps1` (Advanced)

Comprehensive build script with more configuration options.

**Usage:**

```powershell
# Full pipeline with all options
.\scripts\build-local.ps1

# Clean build first
.\scripts\build-local.ps1 -CleanFirst

# Skip tests and coverage
.\scripts\build-local.ps1 -SkipTests

# Custom output directory
.\scripts\build-local.ps1 -OutputPath "./MyTestResults"
```

**Features:**

- ✅ Advanced configuration options
- ✅ Custom output directories
- ✅ Selective test execution
- ✅ Build artifact management

## Prerequisites

- .NET 9 SDK
- PowerShell 7+ (recommended)
- Docker Desktop (for integration tests)

## Quick Start

1. Open PowerShell in the repository root
2. Run the quick build script:
   ```powershell
   .\scripts\build-quick.ps1
   ```
3. View the generated coverage report in your browser

## Output Locations

- **Test Results**: `./TestResults/`
- **Coverage Reports**: `./CoverageReport/`
- **Build Artifacts**: `./src/services/*/bin/` and `./src/services/*/obj/`

---

**Note**: These scripts are designed for local development. For CI/CD pipelines, see the Azure DevOps pipeline configuration in `build/azure-pipelines/`.
