# Build Configuration

This directory contains all build and CI/CD pipeline configurations for the BankSystem Microservices project.

## Directory Structure

```
build/
├── azure-pipelines/
│   ├── ci-build-test.yml          # Main CI pipeline for entire solution
│   ├── security-service.yml       # [Future] Security service CI/CD
│   ├── account-service.yml        # [Future] Account service CI/CD
│   ├── movement-service.yml       # [Future] Movement service CI/CD
│   └── transaction-service.yml    # [Future] Transaction service CI/CD
└── README.md                      # This file
```

## Pipeline Overview

### **Main CI Pipeline** (`ci-build-test.yml`)

- **Purpose**: Build entire solution, run all tests, collect code coverage
- **Triggers**: Changes to `src/services/**/src/**` or `src/services/**/tests/**`
- **Excludes**: Markdown files (`*.md`)
- **Stages**: Build & Test → Code Quality Analysis (SonarQube)

### **Future Service Pipelines**

Individual pipelines will be created for each microservice to enable:

- Independent deployment cycles
- Service-specific testing strategies
- Isolated CI/CD workflows
- Targeted monitoring and alerts

## Usage

### **Setting up the Main CI Pipeline**

1. In Azure DevOps, create a new pipeline
2. Select "Existing Azure Pipelines YAML file"
3. Choose path: `build/azure-pipelines/ci-build-test.yml`
4. Configure variables if using SonarQube (see [CI Documentation](../docs/ci-documentation.md))

### **Local Testing**

```powershell
# Test the pipeline locally (from repository root)
dotnet restore src/BankSystem.sln
dotnet build src/BankSystem.sln --configuration Release --no-restore
dotnet test src/**/tests/**/*.csproj --configuration Release --no-build
```

## Documentation

For detailed information about CI/CD processes, see:

- [CI Documentation](../docs/ci-documentation.md) - Comprehensive pipeline documentation
- [Pipeline Analysis](../PIPELINE_ANALYSIS.md) - Technical analysis and troubleshooting

## Path References

All pipeline files in this directory use paths relative to the **repository root**:

- Solution file: `src/BankSystem.sln`
- Test projects: `src/**/tests/**/*.csproj`
- Coverage settings: `src/coverlet.runsettings`
- Source code: `src/services/`

This ensures pipelines work correctly regardless of where they are executed from.
