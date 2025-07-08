# Build Scripts

This directory contains scripts for building and testing the Bank System Microservices locally.

## Quick Start

```powershell
# Full build, test, and coverage report
.\scripts\build-quick.ps1

# Build only
.\scripts\build-quick.ps1 -BuildOnly

# Tests only
.\scripts\build-quick.ps1 -TestsOnly
```

## Available Scripts

### 🚀 build-quick.ps1 (Recommended)

**Simple, fast script for daily development**

```powershell
.\build-quick.ps1                    # Full pipeline
.\build-quick.ps1 -BuildOnly         # Build with warnings only
.\build-quick.ps1 -TestsOnly         # Run tests only (skip build)
.\build-quick.ps1 -Configuration Debug  # Use Debug configuration
```

**Features:**

- ✅ Builds with warnings in Azure DevOps format (`##[warning]...`)
- ✅ Runs all test projects automatically
- ✅ Generates and opens coverage report
- ✅ Auto-installs ReportGenerator if needed
- ✅ Color-coded output with progress indicators

### ⚙️ build-local.ps1 (Advanced)

**Full-featured script with all pipeline options**

```powershell
.\build-local.ps1                    # Full pipeline with all features
.\build-local.ps1 -ShowWarnings      # Show warnings in Azure DevOps format
.\build-local.ps1 -CleanFirst        # Clean before build
.\build-local.ps1 -SkipTests         # Build only, no tests
.\build-local.ps1 -SkipCoverage      # Run tests without coverage
.\build-local.ps1 -SkipReport        # Don't generate HTML report
.\build-local.ps1 -OutputPath "./CustomResults"  # Custom output directory
```

**Features:**

- ✅ Comprehensive error handling and validation
- ✅ Detailed logging and progress tracking
- ✅ Automatic test project discovery
- ✅ Flexible configuration options
- ✅ Pipeline timing and summary reports
- ✅ Prerequisites checking

### 🪟 build-local.bat (Windows CMD)

**Batch script for cmd.exe users**

```batch
build-local.bat                      # Full pipeline
build-local.bat Release BuildOnly    # Build only
build-local.bat Release TestsOnly    # Tests only
build-local.bat Debug               # Debug configuration
```

**Features:**

- ✅ No PowerShell required
- ✅ Windows CMD compatible
- ✅ Auto-installs dependencies
- ✅ Clear progress indicators

## What These Scripts Do

1. **🔍 Prerequisites Check**

   - Verify .NET 9 SDK installation
   - Check solution and settings files
   - Validate project structure

2. **📦 Package Restore**

   - Restore all NuGet packages
   - Clean restore without unnecessary output

3. **🔨 Build Solution**

   - Build in Release configuration (default)
   - Show warnings in Azure DevOps format (`##[warning]...`)
   - Match CI pipeline warning output exactly
   - No-restore build for speed

4. **🧪 Run Tests**

   - Automatically discover test projects
   - Run unit tests: `Security.Application.UnitTests`, `Security.Domain.UnitTests`
   - Run integration tests: `Security.Infrastructure.IntegrationTests`
   - Collect code coverage in Cobertura format

5. **📊 Generate Coverage Report**
   - Install ReportGenerator if needed
   - Generate interactive HTML report
   - Open report in default browser

## Output Files

After running the scripts, you'll find:

```
TestResults/                         # Test results and coverage
├── [GUID]/
│   ├── coverage.cobertura.xml       # Coverage data
│   └── coverage.opencover.xml       # Alternative format
└── [TestRun].trx                   # Test execution results

CoverageReport/                      # Interactive HTML report
├── index.html                       # Main coverage report
├── summary.html                     # Coverage summary
└── [various HTML files]             # Detailed coverage pages
```

## Matching Azure DevOps Pipeline

These scripts replicate the Azure DevOps pipeline steps:

| Pipeline Step    | Local Equivalent                  |
| ---------------- | --------------------------------- |
| Setup .NET 9 SDK | Prerequisites check               |
| Restore packages | `dotnet restore`                  |
| Build solution   | `dotnet build --verbosity normal` |
| Run tests        | `dotnet test` with coverage       |
| Publish results  | Generate HTML report              |

**Key Differences:**

- Local scripts show immediate output vs. Azure logs
- HTML report opens automatically locally
- Local scripts handle Windows PowerShell limitations
- Prerequisites are checked locally vs. assumed in pipeline

## Azure DevOps Warning Formatting

The PowerShell scripts (`build-quick.ps1` and `build-local.ps1` with `-ShowWarnings`) format build warnings to match Azure DevOps exactly:

**Standard MSBuild Warning:**

```
src/Security.Domain/Entities/User.cs(15,5): warning S3903: Move this type to a named namespace
```

**Azure DevOps Format (Local Scripts):**

```
##[warning]src/Security.Domain/Entities/User.cs(15,5): Warning S3903: Move this type to a named namespace
```

**Features:**

- ✅ Exact match to Azure DevOps CI pipeline output
- ✅ Works with both MSBuild and SonarQube analyzer warnings
- ✅ Color-coded yellow warning text
- ✅ Automatically enabled in `build-quick.ps1`
- ✅ Optional in `build-local.ps1` with `-ShowWarnings` flag

**Note:** The batch script (`build-local.bat`) shows warnings but cannot reformat them due to CMD limitations. Use PowerShell scripts for Azure DevOps formatting.

## Troubleshooting

### Common Issues

**"No test results found"**

- Use specific project paths instead of wildcards
- Check test project naming conventions

**"Coverage files not found"**

- Ensure `coverlet.collector` package is installed
- Verify `src/coverlet.runsettings` exists

**"Build warnings not showing"**

- Use `--verbosity normal` for warnings
- Scripts automatically include proper verbosity

**"PowerShell execution policy error"**

```powershell
# Run once to allow script execution
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Getting Help

```powershell
# View script help
Get-Help .\build-quick.ps1 -Full
Get-Help .\build-local.ps1 -Full
```

### Manual Commands

If scripts fail, run individual commands:

```powershell
# 1. Restore
dotnet restore src/BankSystem.sln

# 2. Build with warnings
dotnet build src/BankSystem.sln --configuration Release --verbosity normal

# 3. Test all unit test projects (recommended)
.\scripts\run-unit-tests.ps1

# OR test individual projects
dotnet test src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj --collect:"XPlat Code Coverage"
dotnet test src/services/Security/tests/Security.Domain.UnitTests/Security.Domain.UnitTests.csproj --collect:"XPlat Code Coverage"

# 4. Generate report
reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./CoverageReport -reporttypes:Html
```

## Performance Tips

- **Use `-BuildOnly`** for quick syntax checking
- **Use `-TestsOnly`** after builds to re-run tests
- **Run scripts from repository root** for best performance
- **Close other applications** during integration tests (Docker containers)

## Integration with Development Workflow

```powershell
# Quick feedback loop
.\build-quick.ps1 -BuildOnly         # Check compilation
.\build-quick.ps1 -TestsOnly         # Run tests after changes
.\build-quick.ps1                    # Full pipeline before commits

# Before pull requests
.\build-local.ps1 -CleanFirst        # Clean build with full validation
```

---

**💡 Tip:** Add these scripts to your PATH or create aliases for even faster access:

```powershell
# PowerShell profile alias
Set-Alias -Name build -Value "c:\path\to\build-quick.ps1"
```
