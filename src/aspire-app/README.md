# Bank System .NET Aspire Configuration

## Overview

This directory contains the .NET Aspire configuration for the Bank System Microservices project. **This setup is intended for local development and testing purposes only**.

## Project Structure

```
aspire-app/
├── AppHost/                 # Aspire application orchestrator
├── ServiceDefaults/         # Shared telemetry and service configuration
└── README.md               # This file
```

## Components

### AppHost

- **Purpose**: Orchestrates all microservices for local development
- **Usage**: Run this project to start all services simultaneously
- **Configuration**: Located in `AppHost.cs` and `appsettings.json`

### ServiceDefaults

- **Purpose**: Provides shared OpenTelemetry configuration for all microservices
- **Key Features**:
  - Trace filtering to reduce telemetry noise
  - Centralized observability configuration
  - Health check instrumentation

## Trace Filtering Configuration

The ServiceDefaults project implements intelligent trace filtering to focus on business operations:

### What Gets Traced ✅

- API endpoints starting with `/api/v*` (e.g., `/api/v1/accounts`, `/api/v2/transactions`)
- All versioned business API calls

### What Gets Excluded ❌

- Health check endpoints (`/health`, `/alive`, `/ready`)
- API documentation (`/swagger`, `/scalar`, `/openapi`)
- Static resources (`/favicon.ico`, `/robots.txt`)
- Metrics endpoints (`/metrics`)
- Infrastructure endpoints

### Benefits

- **Reduced noise**: Eliminates traces from health checks and documentation
- **Cost optimization**: Lower telemetry volume for development
- **Focused monitoring**: Only business operations are traced
- **Better performance**: Reduced instrumentation overhead

## Local Development Usage

### Starting the Application

1. **Set AppHost as startup project**:

   ```bash
   cd src/aspire-app/AppHost
   dotnet run
   ```

2. **Access Aspire Dashboard**:
   - Open browser to `http://localhost:15888`
   - View service status, logs, and traces

### Verifying Trace Filtering

1. **Test API endpoints** (will be traced):

   ```bash
   curl https://localhost:7119/api/v1/accounts
   curl https://localhost:7066/api/v1/auth
   ```

2. **Test infrastructure endpoints** (will NOT be traced):

   ```bash
   curl https://localhost:7119/health
   curl https://localhost:7066/scalar
   ```

3. **Check Aspire Dashboard** → Traces section to verify filtering

## Configuration Files

### AppHost Configuration

- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development-specific settings
- `AppHost.cs`: Service orchestration and resource definitions

### ServiceDefaults Configuration

- `Extensions.cs`: OpenTelemetry and trace filtering logic
- Automatically applied to all microservices

## Important Notes

⚠️ **Local Development Only**: This Aspire configuration is designed for local development and should not be used in production environments.

🔧 **Customization**: To modify trace filtering, edit the `IsExcludedPath` method in `ServiceDefaults/Extensions.cs`.

📊 **Monitoring**: Use the Aspire Dashboard for real-time monitoring of services, dependencies, and telemetry during development.

## Troubleshooting

### Common Issues

**Services not starting**:

- Check port conflicts
- Verify all projects build successfully
- Review Aspire Dashboard logs

**No traces appearing**:

- Ensure API calls start with `/api/v`
- Check OpenTelemetry configuration in ServiceDefaults
- Verify Aspire Dashboard connection

**Performance issues**:

- Trace filtering should reduce overhead
- Monitor resource usage in Aspire Dashboard
- Consider excluding additional paths if needed

## Related Services

This Aspire configuration orchestrates:

- **Security Service**: Authentication and authorization
- **Account Service**: Account management operations
- **Movement Service**: Transaction processing
- **Transaction Service**: Transaction history and reporting
- **Notification Service**: Event notifications
- **Reporting Service**: Analytics and reporting

Each service automatically inherits the trace filtering and telemetry configuration from ServiceDefaults.
