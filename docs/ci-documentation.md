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

#### **Conditions:**

- Runs **ONLY** on `main` branch
- Requires Stage 1 to succeed
- Requires SonarQube service connection

#### **Steps:**

1. **Prepare SonarQube Analysis**

   - Configures SonarQube scanner
   - Sets project key and name
   - Configures coverage report paths

2. **Build for Analysis**

   - Clean build for SonarQube analysis
   - Includes all source code

3. **Run Tests for Analysis**

   - Executes tests again for SonarQube
   - Generates coverage data in required formats

4. **Analyze and Publish**
   - Performs static code analysis
   - Publishes results to SonarQube server

## Variables Configuration

### **Pipeline Variables**

| Variable                | Value                                    | Description                   |
| ----------------------- | ---------------------------------------- | ----------------------------- |
| `buildConfiguration`    | `Release`                                | Build configuration mode      |
| `solution`              | `src/BankSystem.sln`                     | Path to solution file         |
| `testProjectsPattern`   | `src/**/tests/**/*.csproj`               | Pattern to find test projects |
| `coverageReportsFolder` | `$(Agent.TempDirectory)/CoverageReports` | Coverage output directory     |

### **SonarQube Variables (Required for Code Quality stage)**

| Variable             | Description                  | Example                     |
| -------------------- | ---------------------------- | --------------------------- |
| `SONAR_PROJECT_KEY`  | SonarQube project identifier | `banksystem-microservices`  |
| `SONAR_PROJECT_NAME` | Display name in SonarQube    | `Bank System Microservices` |

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

# Install ReportGenerator (optional)
dotnet tool install --global dotnet-reportgenerator-globaltool
```

### **Run Pipeline Steps Locally**

```powershell
# Navigate to repository root
cd c:\Framework_Projects\NET\BankSystemMicroservices

# 1. Restore packages
dotnet restore src/BankSystem.sln

# 2. Build solution
dotnet build src/BankSystem.sln --configuration Release --no-restore

# 3. Run tests with coverage
dotnet test src/**/tests/**/*.csproj `
  --configuration Release `
  --no-build `
  --collect:"XPlat Code Coverage" `
  --results-directory ./TestResults `
  --logger trx `
  --settings src/coverlet.runsettings

# 4. Generate HTML report (optional)
reportgenerator `
  -reports:./TestResults/**/coverage.cobertura.xml `
  -targetdir:./CoverageReport `
  -reporttypes:Html

# 5. View results
start ./CoverageReport/index.html
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

**Solutions:**

```yaml
# Check test project paths
testProjectsPattern: 'src/**/tests/**/*.csproj'

# Alternative patterns
testProjectsPattern: '**/*Tests*.csproj'
testProjectsPattern: '**/*.UnitTests.csproj'
```

#### **Issue**: "Code coverage files not found"

**Causes:**

- Missing `coverlet.collector` NuGet package
- Incorrect coverage settings

**Solutions:**

```xml
<!-- Add to test project -->
<PackageReference Include="coverlet.collector" Version="6.0.4">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
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
