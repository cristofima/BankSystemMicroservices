# Build Configuration

This directory contains all build and CI/CD pipeline configurations for the BankSystem Microservices project.

## Directory Structure

```
build/
├── azure-pipelines/
│   └── ci-build-test.yml                    # Main CI pipeline for entire solution
├── SONARQUBE_COMPLETE_GUIDE.md             # Complete SonarQube integration guide
├── INTEGRATION_TESTING_COMPLETE_GUIDE.md   # Complete integration testing guide
└── README.md                                # This file
```

## Documentation

### **SonarQube Integration** (`SONARQUBE_COMPLETE_GUIDE.md`)

Complete guide covering:

- SonarQube scanner mode configuration (July 2025 updates)
- Azure DevOps test argument conflict resolution
- Coverage file path configuration
- Pipeline YAML configuration
- Troubleshooting and verification steps

### **Integration Testing** (`INTEGRATION_TESTING_COMPLETE_GUIDE.md`)

Complete guide covering:

- Testcontainers setup with SQL Server
- Azure DevOps CI/CD configuration for Docker-based tests
- Local development setup
- Performance considerations and best practices

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
