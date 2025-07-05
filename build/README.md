# Build Configuration

This directory contains the CI/CD pipeline configurations for the BankSystem Microservices project.

## Directory Structure

```
build/
├── azure-pipelines/
│   └── ci-build-test.yml              # Main CI pipeline for entire solution
└── README.md                          # This file
```

## Pipeline Documentation

The main CI/CD pipeline documentation is located in:

- **`docs/ci-documentation.md`** - Complete CI/CD pipeline documentation
- **`docs/sonarqube-integration-guide.md`** - SonarQube integration setup and configuration
- **`docs/guidelines/integration-testing.md`** - Integration testing guidelines and CI integration

## Pipeline File

- **`azure-pipelines/ci-build-test.yml`** - Production-ready CI pipeline that:
  - Builds the entire solution
  - Runs unit and integration tests with code coverage
  - Integrates with SonarQube for code quality analysis
  - Publishes test results and coverage reports

## Usage

To use this pipeline in Azure DevOps:

1. Create a new pipeline
2. Select the repository
3. Choose "Existing Azure Pipelines YAML file"
4. Select `build/azure-pipelines/ci-build-test.yml`
5. Configure required variables (see documentation)

For complete setup instructions, see the documentation files in the `docs/` directory.

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
- [SonarQube Integration Guide](../docs/sonarqube-integration-guide.md) - SonarQube setup and configuration
- [Integration Testing Guide](../docs/guidelines/integration-testing.md) - Integration testing setup

## Path References

All pipeline files in this directory use paths relative to the **repository root**:

- Solution file: `src/BankSystem.sln`
- Test projects: `src/**/tests/**/*.csproj`
- Coverage settings: `src/coverlet.runsettings`
- Source code: `src/services/`

This ensures pipelines work correctly regardless of where they are executed from.
