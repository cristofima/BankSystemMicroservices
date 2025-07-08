# Continuous Integration (CI) Documentation

## Overview

This document describes the CI/CD pipeline configuration for the BankSystem Microservices project. The pipeline is designed to build, test, and analyze the entire solution while providing comprehensive code coverage and quality metrics.

## Pipeline Structure

### **File Location**

- **Pipeline File**: `build/azure-pipelines/ci-build-test.yml`
- **Configuration File**: `src/coverlet.runsettings`

### **Pipeline Organization**

```
build/azure-pipelines/
├── ci-build-test.yml           # Main CI pipeline for entire solution
├── security-service.yml        # [Future] Security service specific pipeline
├── account-service.yml         # [Future] Account service specific pipeline
├── movement-service.yml        # [Future] Movement service specific pipeline
└── transaction-service.yml     # [Future] Transaction service specific pipeline
```

## Trigger Configuration

### **Branch Triggers**

The pipeline triggers on changes to:

- `main` branch
- `develop` branch
- `feature/*` branches

### **Path Triggers**

The pipeline **ONLY** runs when changes are detected in:

- `src/services/**/src/**` - Source code changes
- `src/services/**/tests/**` - Test code changes

### **Path Exclusions**

The pipeline **IGNORES** changes to:

- `src/services/**/*.md` - Markdown documentation files
- `src/services/**/README.md` - README files

### **Example Scenarios**

| Change Type       | Path Example                                                              | Pipeline Triggers? |
| ----------------- | ------------------------------------------------------------------------- | ------------------ |
| ✅ Source Code    | `src/services/Security/src/Security.Api/Controllers/AuthController.cs`    | **YES**            |
| ✅ Test Code      | `src/services/Security/tests/Security.Application.UnitTests/AuthTests.cs` | **YES**            |
| ❌ Documentation  | `src/services/Security/README.md`                                         | **NO**             |
| ❌ Service README | `src/services/Security/src/Security.Api/README.md`                        | **NO**             |
| ✅ Project Files  | `src/services/Security/src/Security.Api/Security.Api.csproj`              | **YES**            |

## Pipeline Stages

### **Stage 1: Build and Test**

#### **Steps:**

1. **Setup .NET 9 SDK**

   - Installs .NET 9.x SDK
   - Configures tool directory

2. **Restore NuGet Packages**

   - Restores all project dependencies
   - Uses NuGet package feeds

3. **Build Solution**

   - Builds entire `BankSystem.sln`
   - Uses Release configuration
   - Skips restore (already done)

4. **Run Tests with Coverage**

   - Executes all test projects matching `src/**/tests/**/*.csproj`
   - Collects code coverage using XPlat Code Coverage
   - Generates coverage in both Cobertura and OpenCover formats
   - Produces test results in TRX format

5. **Publish Test Results**

   - Publishes test results to Azure DevOps
   - Shows pass/fail status and test details
   - Fails pipeline if any tests fail

6. **Publish Code Coverage**

   - Publishes coverage results to Azure DevOps
   - Shows coverage percentages and metrics
   - Links coverage to source code

7. **Generate HTML Coverage Report**
   - Installs ReportGenerator tool
   - Creates interactive HTML coverage report
   - Publishes as pipeline artifact

### **Stage 2: Code Quality Analysis (SonarQube)**

> **📋 Detailed Configuration**: See [SonarQube Integration Guide](./sonarqube-integration-guide.md) for comprehensive setup and troubleshooting.

#### **Conditions:**

- Runs **ONLY** when `SONAR_PROJECT_KEY` variable is set
- Requires Stage 1 to succeed
- Requires SonarQube service connection

#### **Steps:**

1. **Prepare SonarQube Analysis**

   - Configures SonarQube scanner in `dotnet` mode
   - Sets project key and organization
   - Configures auto-discovery with minimal exclusions
   - Excludes auto-generated files (migrations, designer files)

2. **Analyze Code Quality**

   - Performs static code analysis
   - Uses coverage data from previous test runs
   - Analyzes code duplication, maintainability, reliability, security

3. **Publish Quality Gate**
   - Publishes results to SonarQube server
   - Enforces quality gate policies
   - Provides detailed code quality metrics

## Variables Configuration

### **Pipeline Variables**

| Variable                | Value                                    | Description                   |
| ----------------------- | ---------------------------------------- | ----------------------------- |
| `buildConfiguration`    | `Release`                                | Build configuration mode      |
| `solution`              | `src/BankSystem.sln`                     | Path to solution file         |
| `testProjectsPattern`   | `src/**/tests/**/*.csproj`               | Pattern to find test projects |
| `coverageReportsFolder` | `$(Agent.TempDirectory)/CoverageReports` | Coverage output directory     |

### **SonarQube Variables (Required for Code Quality stage)**

> **📋 Complete Setup**: See [SonarQube Integration Guide](./sonarqube-integration-guide.md) for detailed configuration instructions.

| Variable             | Description                  | Example                     |
| -------------------- | ---------------------------- | --------------------------- |
| `SONAR_PROJECT_KEY`  | SonarQube project identifier | `bank-system-microservices` |
| `SONAR_PROJECT_NAME` | Display name in SonarQube    | `Bank System Microservices` |
| `SONAR_ORGANIZATION` | SonarCloud organization      | `your-org-name`             |

## Code Coverage Configuration

### **Coverage Scope**

- **Included Assemblies**: `[Security.*]*`, `[Account.*]*`, `[Movement.*]*`, `[Transaction.*]*`
- **Excluded Assemblies**: `[*.Tests]*`, `[*.UnitTests]*`, `[*.IntegrationTests]*`
- **Excluded Attributes**: `Obsolete`, `GeneratedCode`, `CompilerGenerated`, `ExcludeFromCodeCoverage`
- **Excluded Files**: Migrations, bin/obj folders

### **Coverage Formats**

- **Cobertura**: For Azure DevOps integration
- **OpenCover**: For SonarQube integration

### **Coverage Thresholds**

Currently no minimum thresholds are enforced. Consider adding:

```yaml
# Future enhancement
arguments: >
  --configuration $(buildConfiguration)
  --collect:"XPlat Code Coverage"
  -- RunConfiguration.DisableAppDomain=true
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=80
```

## Artifacts Generated

### **Test Results**

- **Format**: Visual Studio Test Results (`.trx`)
- **Location**: `$(coverageReportsFolder)/**/*.trx`
- **Contains**: Test execution results, timings, failures

### **Code Coverage**

- **Format**: Cobertura XML (`.cobertura.xml`)
- **Location**: `$(coverageReportsFolder)/**/coverage.cobertura.xml`
- **Contains**: Line and branch coverage metrics

### **Coverage Report**

- **Format**: Interactive HTML Report
- **Artifact Name**: `CoverageReport`
- **Contains**: Detailed coverage analysis with drill-down capabilities

## Local Development Testing

### **Prerequisites**

```powershell
# Install .NET 9 SDK
winget install Microsoft.DotNet.SDK.9

# Install ReportGenerator (optional - scripts will auto-install)
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### **Quick Start - Build Scripts**

Three build scripts are provided for different preferences and use cases:

#### **PowerShell Script (Recommended)**

```powershell
# Full build, test, and coverage generation
.\scripts\build-quick.ps1

# Build only
.\scripts\build-quick.ps1 -BuildOnly

# Tests only (skip build)
.\scripts\build-quick.ps1 -TestsOnly

# Debug configuration
.\scripts\build-quick.ps1 -Configuration Debug
```

#### **Advanced PowerShell Script**

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

#### **Batch Script (Windows CMD)**

```batch
# Full build and test
.\scripts\build-local.bat

# Build only
.\scripts\build-local.bat Release BuildOnly

# Tests only
.\scripts\build-local.bat Release TestsOnly

# Debug build
.\scripts\build-local.bat Debug
```

### **Manual Pipeline Steps**

If you prefer to run individual commands (useful for troubleshooting):

```powershell
# Navigate to repository root
cd c:\Framework_Projects\NET\BankSystemMicroservices

# 1. Restore packages
dotnet restore src/BankSystem.sln

# 2. Build solution (shows standard build warnings)
dotnet build src/BankSystem.sln --configuration Release --no-restore --verbosity normal

# 3. Run all unit tests with coverage
dotnet test src/BankSystem.sln `
  --configuration Release `
  --no-build `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults `
  --logger trx `
  --settings src/coverlet.runsettings `
  --filter "FullyQualifiedName~UnitTests"

# 4. Run integration tests with coverage
dotnet test src/BankSystem.sln `
  --configuration Release `
  --no-build `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults `
  --logger trx `
  --settings src/coverlet.runsettings `
  --filter "FullyQualifiedName~IntegrationTests"

# 5. Generate HTML report
reportgenerator `
  -reports:./TestResults/**/coverage.cobertura.xml `
  -targetdir:./CoverageReport `
  -reporttypes:Html

# 6. View results
Start-Process ./CoverageReport/index.html
```

### **Running Specific Test Projects**

```powershell
# Run all unit tests (recommended)
.\scripts\run-unit-tests.ps1

# Or run individual unit test projects:
dotnet test src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj `
  --configuration Release --collect:"XPlat Code Coverage" --settings src/coverlet.runsettings
dotnet test src/services/Security/tests/Security.Domain.UnitTests/Security.Domain.UnitTests.csproj `
  --configuration Release --collect:"XPlat Code Coverage" --settings src/coverlet.runsettings

# Run only integration tests
dotnet test src/services/Security/tests/Security.Infrastructure.IntegrationTests/Security.Infrastructure.IntegrationTests.csproj `
  --configuration Release --collect:"XPlat Code Coverage" --settings src/coverlet.runsettings

# Note: On Windows PowerShell, wildcard patterns like src/**/tests/**/*.csproj don't work
# Use specific project paths or the provided build scripts
```

## Pipeline Setup in Azure DevOps

### **1. Create Pipeline**

1. Go to Azure DevOps project
2. Navigate to **Pipelines** > **New pipeline**
3. Select **Azure Repos Git** (or your source)
4. Select your repository
5. Choose **Existing Azure Pipelines YAML file**
6. Select path: `build/azure-pipelines/ci-build-test.yml`

### **2. Configure Variables**

1. Edit the pipeline
2. Go to **Variables** tab
3. Add variables if using SonarQube:
   - `SONAR_PROJECT_KEY`: Your project key
   - `SONAR_PROJECT_NAME`: Your project name

### **3. Configure Service Connections (Optional)**

For SonarQube integration:

1. Go to **Project Settings** > **Service connections**
2. Create new **SonarQube** service connection
3. Name it `SonarQube` (matches pipeline configuration)

### **4. Branch Policies (Recommended)**

1. Go to **Repos** > **Branches**
2. Select `main` branch > **Branch policies**
3. Add **Build validation**:
   - Build pipeline: Select your CI pipeline
   - Trigger: Automatic
   - Policy requirement: Required

## Monitoring and Troubleshooting

### **Common Issues**

#### **Issue**: "No test results found"

**Causes:**

- Test projects not following naming convention
- Test pattern not matching actual test projects
- PowerShell wildcard patterns not working on Windows

**Solutions:**

```yaml
# Check test project paths
testProjectsPattern: 'src/**/tests/**/*.csproj'

# Alternative patterns for Azure DevOps
testProjectsPattern: '**/*Tests*.csproj'
testProjectsPattern: '**/*.UnitTests.csproj'
```

```powershell
# For local development on Windows, use specific paths:
dotnet test src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj
dotnet test src/services/Security/tests/Security.Domain.UnitTests/Security.Domain.UnitTests.csproj
dotnet test src/services/Security/tests/Security.Infrastructure.IntegrationTests/Security.Infrastructure.IntegrationTests.csproj

# Or use the provided build scripts that handle this automatically
.\scripts\build-quick.ps1
```

#### **Issue**: "Code coverage files not found"

**Causes:**

- Missing `coverlet.collector` NuGet package
- Incorrect coverage settings
- Coverage files generated in unexpected locations

**Solutions:**

```xml
<!-- Add to test project -->
<PackageReference Include="coverlet.collector" Version="6.0.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

```powershell
# Check where coverage files are generated
Get-ChildItem -Path ./TestResults -Recurse -Filter "*.cobertura.xml"

# Ensure you're using the correct settings file
--settings src/coverlet.runsettings
```

#### **Issue**: "PowerShell wildcard patterns don't work"

**Cause:**

- Windows PowerShell doesn't expand `src/**/tests/**/*.csproj` patterns like bash

**Solution:**

```powershell
# ❌ Don't use (fails on Windows PowerShell)
dotnet test src/**/tests/**/*.csproj

# ✅ Use specific paths or run all unit tests with the script
.\scripts\run-unit-tests.ps1

# ✅ Or run individual test projects
dotnet test src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj
dotnet test src/services/Security/tests/Security.Domain.UnitTests/Security.Domain.UnitTests.csproj

# ✅ Or use the provided build scripts
.\scripts\build-quick.ps1
```

#### **Issue**: "Integration tests taking too long"

**Cause:**

- TestContainers spinning up Docker containers
- Database initialization

**Solutions:**

```powershell
# Run only unit tests for faster feedback
.\scripts\run-unit-tests.ps1  # Runs all unit tests with coverage

# Or manually run specific unit test projects
dotnet test src/services/Security/tests/Security.Application.UnitTests/Security.Application.UnitTests.csproj
dotnet test src/services/Security/tests/Security.Domain.UnitTests/Security.Domain.UnitTests.csproj

# Ensure Docker Desktop is running for integration tests
docker info
```

#### **Issue**: "SonarQube analysis fails"

**Causes:**

- Missing service connection
- Incorrect project configuration

**Solutions:**

1. Verify SonarQube service connection exists
2. Check SONAR_PROJECT_KEY variable
3. Ensure SonarQube server is accessible

### **Performance Monitoring**

- **Build Duration**: Target < 10 minutes
- **Test Execution**: Target < 5 minutes
- **Coverage Collection**: Target < 2 minutes

### **Quality Gates**

Consider implementing:

- Minimum test coverage threshold (80%+)
- Zero critical/high security vulnerabilities
- SonarQube quality gate pass
- All tests must pass

## Future Enhancements

### **Service-Specific Pipelines**

Create individual pipelines for each microservice:

- `build/azure-pipelines/security-service.yml`
- `build/azure-pipelines/account-service.yml`
- `build/azure-pipelines/movement-service.yml`
- `build/azure-pipelines/transaction-service.yml`

### **Enhanced Testing**

- Integration tests with test containers
- End-to-end API testing
- Performance testing
- Security vulnerability scanning

### **Deployment Pipelines**

- Development environment deployment
- Staging environment deployment
- Production deployment with approvals
- Blue-green deployment strategies

### **Quality Improvements**

- Enforce code coverage thresholds
- Add security scanning (WhiteSource, Snyk)
- Add dependency vulnerability checks
- Implement automated code reviews

---

**Last Updated**: July 4, 2025  
**Version**: 1.0  
**Maintainer**: Development Team
