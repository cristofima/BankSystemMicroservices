# SonarQube Integration Complete Guide

## Overview

This comprehensive guide covers SonarQube integration for the Bank System Microservices project, including configuration, troubleshooting, and best practices for code quality analysis in Azure DevOps pipelines.

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

#### Conditional Execution Logic

The pipeline now includes intelligent change detection to separate code analysis from coverage collection:

```yaml
# Change detection script
- bash: |
    echo "Detecting changes..."

    # Get list of changed files
    CHANGED_FILES=$(git diff --name-only HEAD~1 HEAD || echo "")
    echo "Changed files: $CHANGED_FILES"

    # Check if any non-documentation files changed
    if [[ -z "$CHANGED_FILES" ]] || echo "$CHANGED_FILES" | grep -qE '\.(cs|csproj|sln|json|yml|yaml)$' && ! echo "$CHANGED_FILES" | grep -qE '^(docs/|.*\.md$|.*README.*|.*LICENSE.*|.*\.txt$)$'; then
      echo "##vso[task.setvariable variable=SkipSonarQube]false"
      echo "Code changes detected - SonarQube analysis will run"
    else
      echo "##vso[task.setvariable variable=SkipSonarQube]true"
      echo "Only documentation changes detected - skipping SonarQube analysis"
    fi

    # Check if coverage-affecting files changed
    if echo "$CHANGED_FILES" | grep -qE '\.(cs)$' && ! echo "$CHANGED_FILES" | grep -qE '^(docs/|.*\.md$|.*README.*|.*LICENSE.*|.*\.txt$|.*\.json$|.*\.yml$|.*\.yaml$|.*\.csproj$|.*\.sln$)$'; then
      echo "##vso[task.setvariable variable=SkipCoverage]false"
      echo "Source code changes detected - coverage collection will run"
    else
      echo "##vso[task.setvariable variable=SkipCoverage]true"
      echo "No source code changes - skipping coverage collection"
    fi
  displayName: "Detect Changes and Set Execution Flags"

# SonarQube preparation
- task: SonarQubePrepare@7
  displayName: "Prepare SonarQube Analysis"
  inputs:
    SonarQube: "SonarQube" # Service connection name
    organization: "$(SONAR_ORGANIZATION)"
    scannerMode: "dotnet" # Critical: Use 'dotnet' mode for .NET projects
    projectKey: "$(SONAR_PROJECT_KEY)"
    projectName: "$(SONAR_PROJECT_NAME)"
    extraProperties: |
      sonar.exclusions=**/bin/**,**/obj/**,**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs,**/*.md,**/README.md,**/docs/**,**/*.json,**/*.yml,**/*.yaml
      sonar.test.exclusions=**/bin/**,**/obj/**,**/TestResults/**
      sonar.coverage.exclusions=**/Program.cs,**/Startup.cs,**/*Extensions.cs,**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs
      sonar.cs.opencover.reportsPaths=$(Agent.TempDirectory)/**/*.xml
      sonar.cs.vstest.reportsPaths=$(Agent.TempDirectory)/**/*.trx
      sonar.sourceEncoding=UTF-8
      sonar.cpd.exclusions=**/Migrations/**,**/*.Designer.cs,**/*ModelSnapshot.cs
  condition: and(succeeded(), ne(variables['SkipSonarQube'], 'true'))
  timeoutInMinutes: 10

# Test execution with conditional coverage
- task: DotNetCoreCLI@2
  displayName: "Run Tests with Coverage"
  inputs:
    command: "test"
    projects: "src/BankSystem.sln"
    arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
    publishTestResults: true
  condition: and(succeeded(), ne(variables['SkipCoverage'], 'true'))

- task: DotNetCoreCLI@2
  displayName: "Run Tests without Coverage"
  inputs:
    command: "test"
    projects: "src/BankSystem.sln"
    arguments: "--configuration $(buildConfiguration)"
    publishTestResults: true
  condition: and(succeeded(), eq(variables['SkipCoverage'], 'true'))

# SonarQube analysis and publishing
- task: SonarQubeAnalyze@7
  displayName: "Run SonarQube Analysis"
  condition: and(succeeded(), ne(variables['SkipSonarQube'], 'true'))
  timeoutInMinutes: 15

- task: SonarQubePublish@7
  displayName: "Publish SonarQube Quality Gate Result"
  inputs:
    pollingTimeoutSec: "300"
  condition: and(succeeded(), ne(variables['SkipSonarQube'], 'true'))
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

The pipeline automatically detects the type of changes and executes accordingly:

#### Scenario 1: Documentation-Only Changes

- **Detection**: Only `.md`, README files, or `docs/` folder changes
- **Execution**: Skip SonarQube analysis entirely
- **Result**: Build succeeds without affecting coverage metrics

#### Scenario 2: Configuration/Build Changes

- **Detection**: Changes to `.csproj`, `.sln`, `.json`, `.yml`, `.yaml` files
- **Execution**: Run SonarQube analysis without coverage collection
- **Result**: Code quality analysis without coverage impact

#### Scenario 3: Source Code Changes

- **Detection**: Changes to `.cs` files (actual source code)
- **Execution**: Full SonarQube analysis with coverage collection
- **Result**: Complete quality analysis including coverage metrics

### Complete Test Step Configuration

```yaml
# Conditional test execution based on change type
- task: DotNetCoreCLI@2
  displayName: "Run Tests with Coverage"
  inputs:
    command: "test"
    projects: "src/BankSystem.sln"
    arguments: '--configuration $(buildConfiguration) --collect:"XPlat Code Coverage" --settings "$(Build.SourcesDirectory)/src/coverlet.runsettings"'
    publishTestResults: true
  condition: and(succeeded(), ne(variables['SkipCoverage'], 'true'))

- task: DotNetCoreCLI@2
  displayName: "Run Tests without Coverage"
  inputs:
    command: "test"
    projects: "src/BankSystem.sln"
    arguments: "--configuration $(buildConfiguration)"
    publishTestResults: true
  condition: and(succeeded(), eq(variables['SkipCoverage'], 'true'))
```

### Key Pipeline Practices

1. **Use Conditional Execution**: Only run SonarQube when variables are set
2. **Set Timeouts**: Prevent hanging with appropriate timeout values
3. **Auto-Discovery**: Let SonarQube discover projects automatically in `dotnet` mode
4. **Minimal Exclusions**: Only exclude necessary files (bin, obj, migrations)

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

1. **Smart Execution**

   ```yaml
   # Conditional execution based on change detection
   condition: and(succeeded(), ne(variables['SkipSonarQube'], 'true'))
   condition: and(succeeded(), ne(variables['SkipCoverage'], 'true'))
   ```

2. **Timeout Management**

   ```yaml
   timeoutInMinutes: 10  # SonarQube Prepare
   timeoutInMinutes: 15  # SonarQube Analyze
   pollingTimeoutSec: '300'  # Quality Gate
   ```

3. **Change Detection Optimization**
   ```bash
   # Efficient file pattern matching
   grep -qE '\.(cs|csproj|sln|json|yml|yaml)$'
   grep -qE '^(docs/|.*\.md$|.*README.*|.*LICENSE.*|.*\.txt$)$'
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

---

**Last Updated**: July 4, 2025  
**Version**: 2.0  
**Status**: ✅ Working Configuration  
**Maintainer**: Development Team
