# Bank System Microservices

<!-- Sonar Qube Badges -->
<table>
  <tr>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=alert_status" alt="Quality Gate Status"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=bugs" alt="Bugs"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=code_smells" alt="Code Smells"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=coverage" alt="Coverage"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=duplicated_lines_density" alt="Duplicated Lines (%)"/></a></td>
  </tr>
  <tr>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=ncloc" alt="Lines of Code"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=reliability_rating" alt="Reliability Rating"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=security_rating" alt="Security Rating"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=sqale_index" alt="Technical Debt"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=sqale_rating" alt="Maintainability Rating"/></a></td>
    <td><a href="https://sonarcloud.io/summary/new_code?id=cristofima_BankSystemMicroservices"><img src="https://sonarcloud.io/api/project_badges/measure?project=cristofima_BankSystemMicroservices&metric=vulnerabilities" alt="Vulnerabilities"/></a></td>
  </tr>
</table>

A modern, cloud-native banking system built with .NET 9 microservices architecture, designed for Azure deployment with Clean Architecture, CQRS, and Event-Driven Architecture patterns.

## 🏗️ Architecture Overview

This system implements a distributed banking platform using microservices that communicate through Azure Service Bus events, following Domain-Driven Design (DDD) principles and the CQRS pattern for optimal scalability and maintainability.

### Core Microservices

- **🔐 Security Service**: Handles authentication, authorization, and user management
- **🏦 Account Service**: Manages customer accounts, balances, and account operations
- **💸 Transaction Service**: Processes financial transactions (deposits, withdrawals) - Write operations
- **📊 Movement Service**: Provides transaction history and reporting - Read operations

### Architecture Patterns

- **Clean Architecture**: Clear separation of concerns across layers
- **CQRS (Command Query Responsibility Segregation)**: Separate read and write operations
- **Event-Driven Architecture**: Asynchronous communication via Azure Service Bus
- **Domain-Driven Design**: Rich domain models with business logic encapsulation
- **Microservices**: Independently deployable and scalable services

## 🚀 Technology Stack

### Backend

- **.NET 9**: Latest framework with improved performance and features
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for data access
- **MediatR**: CQRS and Mediator pattern implementation
- **FluentValidation**: Input validation
- **AutoMapper**: Object-to-object mapping
- **Serilog**: Structured logging

### Azure Services

- **Azure Service Bus**: Message broker for event-driven communication
- **Azure SQL Database**: Primary database for transactions and accounts
- **Azure Cosmos DB**: Document database for movement history (read-optimized)
- **Azure Key Vault**: Secrets and configuration management
- **Azure Application Insights**: Monitoring and telemetry
- **Azure API Management**: API Gateway and management
- **Azure Container Apps**: Container hosting platform

### Development Tools

- **Docker**: Containerization
- **Terraform/Bicep**: Infrastructure as Code
- **Azure DevOps**: CI/CD pipelines
- **xUnit**: Unit testing framework
- **FluentAssertions**: Assertion library

## 🏛️ System Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────────┐
│   Client App    │──▶│  API Management  │───▶│   Microservices     │
│  (Angular)      │    │   (Gateway)      │    │                     │
└─────────────────┘    └──────────────────┘    └─────────────────────┘
                                                           │
                              ┌────────────────────────────┼────────────────────────────┐
                              │                            │                            │
                              ▼                            ▼                            ▼
                    ┌─────────────────┐        ┌─────────────────┐        ┌─────────────────┐
                    │   Security      │        │   Account       │        │  Transaction    │
                    │   Service       │        │   Service       │        │   Service       │
                    └─────────────────┘        └─────────────────┘        └─────────────────┘
                              │                            │                            │
                              └────────────────────────────┼────────────────────────────┘
                                                           │
                                           ┌───────────────▼───────────────┐
                                           │     Azure Service Bus         │
                                           │    (Event Distribution)       │
                                           └───────────────┬───────────────┘
                                                           │
                                                           ▼
                                                ┌─────────────────┐
                                                │   Movement      │
                                                │   Service       │
                                                └─────────────────┘
```

## 🔄 Event-Driven Flow

### Transaction Processing Flow

1. **Client** initiates a deposit/withdrawal request
2. **API Gateway** routes to Transaction Service
3. **Transaction Service** validates and processes the transaction
4. **Transaction Service** publishes `TransactionCreatedEvent`
5. **Account Service** subscribes to update account balance
6. **Movement Service** subscribes to create movement history record

### Benefits

- **Loose Coupling**: Services communicate through events
- **Scalability**: Each service can scale independently
- **Resilience**: Failure in one service doesn't affect others
- **Eventual Consistency**: Data consistency across services

## 📁 Project Structure

```
/BankSystemMicroservices/
├── src/
│   ├── BankSystem.sln
│   ├── services/
│   │   ├── Security/           # Authentication & Authorization
│   │   ├── Account/           # Account Management
│   │   ├── Transaction/       # Transaction Processing
│   │   └── Movement/          # Movement History & Reporting
│   ├── shared/                # Common components
│   └── client/               # Web application
├── docs/                     # Documentation
├── iac/                      # Infrastructure as Code
├── tests/                    # Integration tests
└── build/                    # CI/CD configurations
```

## 🚦 Getting Started

### Prerequisites

- .NET 9 SDK
- Docker Desktop
- Azure CLI
- Visual Studio 2022 or VS Code

### Local Development Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/your-org/bank-system-microservices.git
   cd bank-system-microservices
   ```

2. **Start infrastructure services**

   ```bash
   docker-compose -f docker-compose.infrastructure.yml up -d
   ```

3. **Update connection strings**

   ```bash
   # Update appsettings.Development.json in each service
   ```

4. **Run database migrations**

   ```bash
   dotnet ef database update --project src/services/Account/src/Account.Infrastructure
   dotnet ef database update --project src/services/Transaction/src/Transaction.Infrastructure
   ```

5. **Start services**

   ```bash
   # Terminal 1 - Security Service
   dotnet run --project src/services/Security/src/Security.Api

   # Terminal 2 - Account Service
   dotnet run --project src/services/Account/src/Account.Api

   # Terminal 3 - Transaction Service
   dotnet run --project src/services/Transaction/src/Transaction.Api

   # Terminal 4 - Movement Service
   dotnet run --project src/services/Movement/src/Movement.Api
   ```

## 🔧 Configuration

### Environment Variables

```bash
# Database Connections
CONNECTIONSTRINGS__DEFAULTCONNECTION="Server=localhost;Database=BankSystem;Trusted_Connection=true;"

# Azure Service Bus
AZURE__SERVICEBUS__CONNECTIONSTRING="Endpoint=sb://your-namespace.servicebus.windows.net/..."

# JWT Settings
JWT__KEY="your-super-secret-key"
JWT__ISSUER="https://localhost:5001"
JWT__AUDIENCE="bank-system-api"
```

## 📊 API Documentation

Each microservice exposes its own OpenAPI/Scalar documentation:

- **Security API**: `https://localhost:5001/scalar`
- **Account API**: `https://localhost:5002/scalar`
- **Transaction API**: `https://localhost:5003/scalar`
- **Movement API**: `https://localhost:5004/scalar`

## 🧪 Testing

### Run Unit Tests

**Basic unit test execution:**

```bash
dotnet test
```

**Run unit tests with code coverage (recommended):**

```powershell
# Use the provided PowerShell script
./scripts/run-unit-tests.ps1
```

This will:

- Run all unit test projects in the solution
- Generate code coverage reports in multiple formats (HTML, Cobertura, JSON)
- Open the HTML coverage report automatically
- Results are saved to `TestResults/` directory

**Manual coverage command:**

```bash
dotnet test --configuration Debug --collect:"XPlat Code Coverage" --settings coverlet.runsettings --results-directory TestResults /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=TestResults/coverage.cobertura.xml
```

### Run Integration Tests

```bash
dotnet test --configuration Release --filter Category=Integration
```

### Run Load Tests

```bash
# Using k6 or Azure Load Testing
k6 run tests/load/transaction-load-test.js
```

### Available Test Scripts

See [scripts/README.md](scripts/README.md) for detailed information about available build and test scripts.

## 🚀 Deployment

### Azure Deployment

```bash
# Deploy infrastructure
terraform apply -var-file="environments/prod.tfvars"

# Deploy applications
az acr build --registry bankSystemRegistry --image security-service:latest ./src/services/Security
az containerapp update --name security-service --image bankSystemRegistry.azurecr.io/security-service:latest
```

## 🔍 Monitoring & Observability

- **Application Insights**: Performance monitoring and telemetry
- **Azure Monitor**: Infrastructure monitoring
- **Structured Logging**: Centralized logging with Serilog
- **Health Checks**: Service health monitoring
- **Distributed Tracing**: Request flow tracking

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow the [Development Guidelines](docs/dotnet-development-guidelines.md)
4. Commit your changes (`git commit -m 'Add amazing feature'`)
5. Push to the branch (`git push origin feature/amazing-feature`)
6. Open a Pull Request

## 📚 Documentation

- [.NET Development Guidelines](docs/dotnet-development-guidelines.md)
- [API Documentation](docs/api-documentation.md)
- [Architecture Decision Records](docs/adr/)
- [Deployment Guide](docs/deployment-guide.md)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Related Services

- [Security Service](src/services/Security/README.md)
- [Account Service](src/services/Account/README.md)
- [Transaction Service](src/services/Transaction/README.md)
- [Movement Service](src/services/Movement/README.md)
