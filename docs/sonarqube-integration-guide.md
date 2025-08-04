# SonarQube Integration Guide

## Overview

This guide provides comprehensive instructions for integrating SonarQube analysis into the Bank System Microservices CI/CD pipeline. The integration ensures consistent code quality analysis with proper coverage reporting while maintaining pipeline performance.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Configuration Files](#configuration-files)
3. [Pipeline Integration](#pipeline-integration)
4. [Troubleshooting](#troubleshooting)
5. [Best Practices](#best-practices)
6. [Maintenance](#maintenance)

## Quick Start

### Prerequisites

- SonarQube/SonarCloud account and organization
- Azure DevOps service connection to SonarQube
- .NET 9 project with test projects

### Required Variables

Set these variables in your Azure DevOps pipeline:

```yaml
variables:
  SONAR_PROJECT_KEY: "bank-system-microservices"
  SONAR_PROJECT_NAME: "Bank System Microservices"
  SONAR_ORGANIZATION: "your-sonarcloud-organization"
```

## Configuration Files

### 1. Project Configuration

#### Required Variables for Azure DevOps

```yaml
variables:
  SONAR_PROJECT_KEY: "bank-system-microservices"
  SONAR_PROJECT_NAME: "Bank System Microservices"
  SONAR_ORGANIZATION: "your-sonarcloud-organization"
```

#### SonarQube Project Properties

```properties
# Basic project settings
sonar.projectKey=bank-system-microservices
sonar.projectName=Bank System Microservices
sonar.projectVersion=1.0

# Source and test directories (auto-discovery recommended)
sonar.sources=src
sonar.tests=src
sonar.test.inclusions=**/tests/**/*Tests.cs

# Code coverage settings
sonar.cs.opencover.reportsPaths=**/coverage.opencover.xml,$(Agent.TempDirectory)/**/*.xml
sonar.cs.vstest.reportsPaths=**/TestResults/*.trx,$(Agent.TempDirectory)/**/*.trx

# Coverage exclusions
sonar.coverage.exclusions=**/Program.cs,**/Startup.cs,**/*Extensions.cs,**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs,**/obj/**,**/bin/**

# Global file exclusions
sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs,**/*.md,**/README.md,**/docs/**,**/*.json,**/*.yml,**/*.yaml

# Test exclusions
sonar.test.exclusions=**/bin/**,**/obj/**,**/TestResults/**

# Duplicate detection exclusions
sonar.cpd.exclusions=**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs

# Quality gate settings
sonar.newCodePeriod.type=REFERENCE_BRANCH
sonar.newCodePeriod.value=main

# Branch configuration
sonar.branch.name=main
sonar.branch.target=main
```

### 2. Pipeline Configuration (`ci-build-test.yml`)

#### Simple SonarQube Preparation

The pipeline now uses a single, always-running SonarQube preparation task that consistently collects coverage data:

```yaml
# SonarQube preparation - runs when SonarQube service connection is available
- task: SonarQubePrepare@7
  displayName: 'Prepare SonarQube Analysis'
  condition: ne(variables['SONAR_PROJECT_KEY'], '')
  inputs:
    SonarQube: 'SonarCloud'
    scannerMode: 'MSBuild'
    projectKey: '$(SONAR_PROJECT_KEY)'
    projectName: '$(SONAR_PROJECT_NAME)'
    organization: '$(SONAR_ORGANIZATION)'
    extraProperties: |
      sonar.cs.opencover.reportsPaths=$(Agent.TempDirectory)/**/*.xml
    COVERAGE_AFFECTING_CHANGES=$(echo "$CHANGED_FILES" | grep -E '\.(cs|fs|vb)$' | grep -E '^src/(services|shared)/' | grep -v '/bin/' | grep -v '/obj/' | wc -l)

    echo "Number of code changes: $CODE_CHANGES"
    echo "Number of coverage-affecting changes: $COVERAGE_AFFECTING_CHANGES"

    # Strategy: Always collect coverage for unit tests, but only send to SonarQube when needed
    if [ "$CODE_CHANGES" -eq 0 ]; then
      echo "Only documentation files changed - skipping SonarQube analysis entirely"
      echo "##vso[task.setvariable variable=SkipSonarQube]true"
      echo "##vso[task.setvariable variable=SendCoverageToSonar]false"
      echo "##vso[task.setvariable variable=RunWithoutCoverage]false"
    else
      echo "Code files changed - running SonarQube analysis"
      echo "##vso[task.setvariable variable=SkipSonarQube]false"

      # Determine SonarQube execution mode based on coverage-affecting changes
      if [ "$COVERAGE_AFFECTING_CHANGES" -eq 0 ]; then
        echo "No coverage-affecting changes - running SonarQube WITHOUT coverage data"
        echo "##vso[task.setvariable variable=SendCoverageToSonar]false"
        echo "##vso[task.setvariable variable=RunWithoutCoverage]true"
      else
        echo "Coverage-affecting changes detected - running SonarQube WITH coverage data"
        echo "##vso[task.setvariable variable=SendCoverageToSonar]true"
        echo "##vso[task.setvariable variable=RunWithoutCoverage]false"
      fi
    fi
  displayName: "Analyze Changes for Pipeline Optimization"

# SonarQube preparation
- task: SonarQubePrepare@7
  displayName: "Prepare SonarQube Analysis (WITH Coverage Data)"
  inputs:
    SonarQube: "SonarQube" # Service connection name
    organization: "$(SONAR_ORGANIZATION)"
    scannerMode: "dotnet" # Critical: Use 'dotnet' mode for .NET projects
```

#### Test Execution with Coverage

Tests are always run with coverage collection for consistent SonarQube reporting:

```yaml
# Test execution - always with coverage collection
- task: DotNetCoreCLI@2
  displayName: "Run Tests with Coverage"
  inputs:
    command: "test"
    projects: "src/BankSystem.sln"
    arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
    publishTestResults: true

# SonarQube analysis and publishing
- task: SonarQubeAnalyze@7
  displayName: "Run SonarQube Analysis"
  condition: ne(variables['SONAR_PROJECT_KEY'], '')
  timeoutInMinutes: 15

- task: SonarQubePublish@7
  displayName: "Publish SonarQube Quality Gate Result"
  inputs:
    pollingTimeoutSec: "300"
  condition: ne(variables['SONAR_PROJECT_KEY'], '')
```

### 3. Code Coverage Configuration (`coverlet.runsettings`)

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>cobertura,opencover</Format>
          <Exclude>[*.Tests]*,[*.UnitTests]*,[*.IntegrationTests]*</Exclude>
          <IncludeTestAssembly>false</IncludeTestAssembly>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

## Pipeline Integration

### Smart Execution Logic

The pipeline automatically detects the type of changes and executes accordingly using a three-variable control system (`SkipSonarQube`, `SendCoverageToSonar`, `RunWithoutCoverage`):

#### Scenario 1: Documentation-Only Changes

- **Detection**: Only `.md`, README files, or `docs/` folder changes
- **Execution**: Skip SonarQube analysis entirely (`SkipSonarQube = true`)
- **Result**: Build succeeds without affecting coverage metrics

#### Scenario 2: Configuration/Build Changes (No Coverage-Affecting Code)

- **Detection**: Changes to `.csproj`, `.sln`, `.json`, `.yml`, `.yaml` files without source code changes
- **Execution**: Run SonarQube analysis without sending coverage data (`RunWithoutCoverage = true`)
- **Result**: Code quality analysis without coverage impact, preserving existing coverage metrics

#### Scenario 3: Source Code Changes (Coverage-Affecting)

- **Detection**: Changes to `.cs` files in `src/services/` or `src/shared/` directories
- **Execution**: Full SonarQube analysis with coverage collection and reporting (`SendCoverageToSonar = true`)
- **Result**: Complete quality analysis including updated coverage metrics

### Complete Test Step Configuration

```yaml
# Simplified test execution - always collect coverage
- task: DotNetCoreCLI@2
  displayName: "Run Unit Tests with Coverage"
  inputs:
    command: "test"
    projects: "**/*UnitTests.csproj"
    arguments: '--no-build --no-restore --collect "XPlat Code Coverage" --logger trx --results-directory $(Agent.TempDirectory)'
    publishTestResults: true
  continueOnError: false
  timeoutInMinutes: 15
```

The test step has been simplified to always collect coverage data, ensuring consistent coverage collection regardless of what files are changed. The coverage data is always collected but only sent to SonarQube when `SendCoverageToSonar` is set to `true`.

### Key Pipeline Practices

1. **Use Three-Variable Control**: Employ `SkipSonarQube`, `SendCoverageToSonar`, and `RunWithoutCoverage` for precise conditional execution
2. **Dual SonarQube Preparation**: Use separate preparation tasks - WITH coverage when any code requires coverage analysis, WITHOUT coverage only when ALL changes don't require coverage
3. **Set Timeouts**: Prevent hanging with appropriate timeout values
4. **Auto-Discovery**: Let SonarQube discover projects automatically in `dotnet` mode
5. **Minimal Exclusions**: Only exclude necessary files (bin, obj, migrations)
6. **Always Collect Coverage**: Simplify pipeline by always collecting coverage data, then controlling what gets sent to SonarQube

## Best Practices

### Pipeline Configuration

1. **Simple Configuration**: Use a single SonarQube preparation task for consistent coverage reporting
2. **Always Collect Coverage**: Run tests with coverage collection enabled to maintain continuous metrics
3. **Appropriate Exclusions**: Configure proper file exclusions to focus on meaningful code analysis
4. **Robust Error Handling**: Include timeout configurations and proper condition checks
5. **Service Connection Management**: Ensure SonarQube service connection is properly configured
6. **Clear Display Names**: Use descriptive task names for better pipeline visibility
7. **Consistent Variables**: Use standardized variable names for project configuration

## Troubleshooting

### Common Issues and Solutions

#### Issue 1: "File can't be indexed twice"

**Symptoms:**

```
ERROR: File src/services/Security/src/Security.Infrastructure/DependencyInjection.cs can't be indexed twice
```

**Root Cause:**

- Overlapping source and test paths
- Manual source specification conflicting with auto-discovery

**Solution:**

```yaml
# ❌ Don't specify both sources and tests manually
sonar.sources=src/services/Security/src,src/services/Account/src
sonar.tests=src/services/Security/tests,src/services/Account/tests

# ✅ Let auto-discovery work, only specify exclusions
sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**
sonar.test.exclusions=**/bin/**,**/obj/**
```

#### Issue 2: "Duplicate arguments error"

**Symptoms:**

```
Duplicate '--results-directory' arguments
Duplicate '--logger' arguments
```

**Root Cause:**
Azure DevOps automatically injects `--logger trx` and `--results-directory` arguments

**Solution:**

```yaml
# ✅ Only specify coverage collection arguments
arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
# ❌ Don't manually specify these (Azure DevOps adds them automatically)
# --logger trx
# --results-directory /path/to/results
```

#### Issue 3: "Coverage files not found"

**Symptoms:**

```
No coverage files found at specified paths
```

**Root Cause:**

- Coverage files generated in different location than expected
- Incorrect path specifications

**Solution:**

```yaml
# Use multiple search paths
sonar.cs.opencover.reportsPaths=$(Agent.TempDirectory)/**/*.xml,$(resultsDirectory)/**/*.xml,**/TestResults/**/coverage.opencover.xml
```

#### Issue 4: "Scanner mode deprecated"

**Symptoms:**

```
MSBuild scanner mode is deprecated
```

**Solution:**

```yaml
# ✅ Use dotnet mode for .NET projects
scannerMode: "dotnet"
# ❌ Avoid deprecated modes
# scannerMode: 'MSBuild'
# scannerMode: 'CLI'
```

#### Issue 5: "Migration duplication detected"

**Symptoms:**

```
Code duplication detected in migration files
```

**Root Cause:**
Entity Framework migrations contain repetitive code patterns

**Solution:**

```yaml
# Exclude auto-generated files from analysis
sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs
```

### Debugging Steps

1. **Check Service Connection**

   ```yaml
   # Verify SonarQube service connection exists and is named correctly
   SonarQube: "SonarQube" # Must match service connection name
   ```

2. **Validate Variables**

   ```yaml
   # Check that required variables are set
   condition: and(succeeded(), ne(variables['SONAR_PROJECT_KEY'], ''))
   ```

3. **Review File Patterns**

   ```bash
   # List files that will be analyzed
   find $(Build.SourcesDirectory) -name "*.cs" -not -path "*/bin/*" -not -path "*/obj/*"
   ```

4. **Check Coverage Generation**
   ```bash
   # Verify coverage files are generated
   find $(Agent.TempDirectory) -name "*.xml" -type f | grep -i coverage
   ```

## Best Practices

### Configuration Management

1. **Use Auto-Discovery**

   - Let SonarQube automatically discover projects in `dotnet` mode
   - Avoid manual source/test path specifications
   - Only specify exclusions when necessary

2. **Minimal Exclusions**

   ```yaml
   # Only exclude what's necessary
   sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs
   ```

3. **Consistent Naming**
   - Use clear, descriptive project keys
   - Follow organization naming conventions
   - Include version information where applicable

### Performance Optimization

1. **Simple Execution**

   ```yaml
   # Straightforward conditional execution
   condition: ne(variables['SONAR_PROJECT_KEY'], '')
   ```

2. **Timeout Management**

   ```yaml
   timeoutInMinutes: 10  # SonarQube Prepare
   timeoutInMinutes: 15  # SonarQube Analyze
   pollingTimeoutSec: '300'  # Quality Gate
   ```

3. **Consistent Coverage Collection**
   ```yaml
   # Always collect coverage for reliable metrics
   arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
   ```

### Quality Gates

1. **Configure Quality Gates**

   - Set appropriate coverage thresholds (80%+)
   - Define maintainability ratings
   - Set security hotspot requirements
   - Configure reliability standards

2. **Branch Policies**
   ```yaml
   # Different settings for different branches
   - task: SonarQubePrepare@7
     condition: eq(variables['Build.SourceBranch'], 'refs/heads/main')
   ```

### Security

1. **Protect Sensitive Data**

   ```yaml
   # Use variable groups for sensitive data
   variables:
     - group: SonarQube-Settings
   ```

2. **Service Connection Security**
   - Use service principals with minimal permissions
   - Regularly rotate authentication tokens
   - Limit access to specific projects

## Maintenance

### Regular Tasks

1. **Monitor Quality Gates**

   - Review failed quality gates weekly
   - Address security hotspots promptly
   - Monitor technical debt trends

2. **Update Dependencies**

   ```xml
   <!-- Keep SonarQube tasks updated -->
   <PackageReference Include="SonarAnalyzer.CSharp" Version="Latest" />
   ```

3. **Review Exclusions**
   - Periodically review exclusion patterns
   - Remove unnecessary exclusions
   - Add exclusions for new auto-generated code

### Performance Monitoring

```yaml
# Add timing information
- bash: |
    echo "SonarQube Analysis Started: $(date)"
  displayName: "Log Analysis Start Time"

- task: SonarQubeAnalyze@7
  # ... configuration

- bash: |
    echo "SonarQube Analysis Completed: $(date)"
  displayName: "Log Analysis End Time"
```

### Troubleshooting Checklist

- [ ] Service connection configured and accessible
- [ ] Required variables set (SONAR_PROJECT_KEY, etc.)
- [ ] Scanner mode set to 'dotnet'
- [ ] No overlapping source/test paths
- [ ] Coverage files being generated
- [ ] Exclusion patterns correct
- [ ] Timeout values appropriate
- [ ] Quality gate configured
- [ ] Branch policies in place

## Advanced Configuration

### Multi-Service Analysis

```yaml
# Analyze multiple services in one pipeline
extraProperties: |
  sonar.organization=$(SONAR_ORGANIZATION)
  sonar.modules=security,account,movement,transaction
  security.sonar.projectBaseDir=src/services/Security
  account.sonar.projectBaseDir=src/services/Account
  movement.sonar.projectBaseDir=src/services/Movement
  transaction.sonar.projectBaseDir=src/services/Transaction
```

### Custom Quality Profiles

```yaml
# Use custom quality profiles
extraProperties: |
  sonar.profile=Custom .NET Profile
  sonar.cs.profile=Custom C# Profile
```

### Integration with Pull Requests

```yaml
# PR-specific analysis
extraProperties: |
  sonar.pullrequest.key=$(System.PullRequest.PullRequestNumber)
  sonar.pullrequest.branch=$(System.PullRequest.SourceBranch)
  sonar.pullrequest.base=$(System.PullRequest.TargetBranch)
```

## Resources

- [SonarQube Documentation](https://docs.sonarqube.org/)
- [Azure DevOps SonarQube Extension](https://marketplace.visualstudio.com/items?itemName=SonarSource.sonarqube)
- [SonarCloud for Azure DevOps](https://sonarcloud.io/documentation/integrations/azuredevops/)
- [.NET Code Coverage](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-code-coverage)

**Last Updated**: July 4, 2025  
**Version**: 2.0  
**Status**: ✅ Working Configuration  
**Maintainer**: Development Team
