# Security Service

The Security Service is responsible for authentication, authorization, and user management within the Bank System Microservices architecture. It provides JWT-based authentication and implements secure user registration, login, and password management functionality.

## 🎯 Service Overview

### Responsibilities

- **User Authentication**: Secure login with JWT token generation
- **User Registration**: New user account creation with validation
- **Password Management**: Password reset and recovery functionality
- **Authorization**: Role-based access control (RBAC)
- **Token Management**: JWT token validation and refresh
- **User Profile Management**: User information updates

### Domain Boundaries

- User identity and credentials
- Authentication sessions
- Security policies and roles
- Password policies and validation

## 🏗️ Architecture

### Clean Architecture Layers

```
Security.Api/               # Presentation Layer
├── Controllers/           # API Controllers
├── Middleware/           # Authentication middleware
├── Extensions/           # Service extensions
└── Program.cs           # Application startup

Security.Application/       # Application Layer
├── Commands/            # CQRS Commands (Register, Login, ResetPassword)
├── Queries/            # CQRS Queries (GetUser, ValidateToken)
├── Handlers/           # Command & Query Handlers
├── DTOs/              # Data Transfer Objects
├── Interfaces/        # Application Interfaces
├── Validators/        # FluentValidation Validators
└── Mappers/          # AutoMapper Profiles

Security.Domain/           # Domain Layer
├── Entities/            # Domain Entities (ApplicationUser)
├── ValueObjects/       # Value Objects (Email, Password)
├── Events/            # Domain Events
├── Enums/            # Domain Enumerations
└── Exceptions/       # Domain Exceptions

Security.Infrastructure/   # Infrastructure Layer
├── Data/              # EF Core DbContext
├── Services/          # External Service Integrations
├── Identity/          # ASP.NET Core Identity Configuration
└── Repositories/      # Repository Implementations
```

## 🔧 Features

### Authentication Features

- **JWT Token Authentication**: Secure token-based authentication
- **Multi-Factor Authentication**: Optional 2FA support
- **Session Management**: Token expiration and refresh
- **Password Policies**: Configurable password complexity requirements

### User Management Features

- **User Registration**: Account creation with email verification
- **Profile Management**: User information updates
- **Password Reset**: Secure password recovery flow
- **Account Lockout**: Brute force protection

### Security Features

- **Password Hashing**: BCrypt password hashing
- **Rate Limiting**: API endpoint protection
- **CORS Configuration**: Cross-origin request handling
- **Security Headers**: Implementation of security best practices

## 🔌 API Endpoints

### Authentication Endpoints

#### POST /api/auth/register

Register a new user account.

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+1234567890"
}
```

**Response:**

```json
{
  "userId": "guid",
  "email": "user@example.com",
  "message": "User registered successfully"
}
```

#### POST /api/auth/login

Authenticate user and receive JWT token.

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string",
  "expiresAt": "2024-12-31T23:59:59Z",
  "user": {
    "id": "guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe"
  }
}
```

#### POST /api/auth/refresh

Refresh JWT token using refresh token.

**Request Body:**

```json
{
  "token": "expired_jwt_token",
  "refreshToken": "valid_refresh_token"
}
```

#### POST /api/auth/forgot-password

Initiate password reset process.

**Request Body:**

```json
{
  "email": "user@example.com"
}
```

#### POST /api/auth/reset-password

Reset password using reset token.

**Request Body:**

```json
{
  "token": "reset_token",
  "email": "user@example.com",
  "newPassword": "NewSecurePassword123!"
}
```

## 🗄️ Data Model

### ApplicationUser Entity

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndDate { get; set; }
}
```

### Database Schema

- **AspNetUsers**: User accounts and profiles
- **AspNetUserTokens**: JWT refresh tokens
- **AspNetUserLogins**: External login providers
- **AspNetRoles**: User roles and permissions
- **AspNetUserRoles**: User-role relationships

## ⚙️ Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BankSystem_Security;Trusted_Connection=true;"
  },
  "Jwt": {
    "Key": "your-super-secret-jwt-key-here",
    "Issuer": "https://localhost:5001",
    "Audience": "bank-system-api",
    "ExpiryInMinutes": 60,
    "RefreshTokenExpiryInDays": 7
  },
  "PasswordPolicy": {
    "RequiredLength": 8,
    "RequireNonAlphanumeric": true,
    "RequireLowercase": true,
    "RequireUppercase": true,
    "RequireDigit": true
  },
  "Lockout": {
    "DefaultLockoutTimeSpan": "00:05:00",
    "MaxFailedAccessAttempts": 5,
    "AllowedForNewUsers": true
  }
}
```

### Environment Variables

```bash
# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION="Server=localhost;Database=BankSystem_Security;..."

# JWT Configuration
JWT__KEY="your-super-secret-jwt-key"
JWT__ISSUER="https://your-api.com"
JWT__AUDIENCE="bank-system-api"

# External Services
AZURE__KEYVAULT__VAULTURL="https://your-keyvault.vault.azure.net/"
```

## 🔐 Security Implementation

### Password Hashing

```csharp
public class PasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
    }
}
```

### JWT Token Generation

```csharp
public class TokenService : ITokenService
{
    public async Task<TokenDto> GenerateTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("jti", Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
            signingCredentials: credentials
        );

        return new TokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = await GenerateRefreshTokenAsync(user),
            ExpiresAt = token.ValidTo
        };
    }
}
```

## 🧪 Testing

### Unit Tests

```csharp
[TestFixture]
public class LoginCommandHandlerTests
{
    [Test]
    public async Task Handle_ValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "Password123!");
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.Not.Empty);
        Assert.That(result.User.Email, Is.EqualTo("user@test.com"));
    }

    [Test]
    public void Handle_InvalidCredentials_ShouldThrowException()
    {
        // Arrange
        var command = new LoginCommand("user@test.com", "WrongPassword");
        var handler = CreateHandler();

        // Act & Assert
        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));
    }
}
```

### Integration Tests

```csharp
[TestFixture]
public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Test]
    public async Task Register_ValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var request = new RegisterDto
        {
            Email = "newuser@test.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }
}
```

## 📊 Monitoring & Observability

### Metrics

- Authentication success/failure rates
- Token generation frequency
- Password reset requests
- Failed login attempts
- User registration trends

### Logging Events

```csharp
public static class SecurityEvents
{
    public static readonly EventId UserRegistered = new(1001, "UserRegistered");
    public static readonly EventId UserLoggedIn = new(1002, "UserLoggedIn");
    public static readonly EventId LoginFailed = new(1003, "LoginFailed");
    public static readonly EventId PasswordReset = new(1004, "PasswordReset");
    public static readonly EventId AccountLocked = new(1005, "AccountLocked");
}
```

### Health Checks

- Database connectivity
- JWT key availability
- External service dependencies
- Identity provider status

## 🚀 Deployment

### Docker Configuration

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Security.Api/Security.Api.csproj", "Security.Api/"]
COPY ["Security.Application/Security.Application.csproj", "Security.Application/"]
COPY ["Security.Domain/Security.Domain.csproj", "Security.Domain/"]
COPY ["Security.Infrastructure/Security.Infrastructure.csproj", "Security.Infrastructure/"]

RUN dotnet restore "Security.Api/Security.Api.csproj"
COPY . .
WORKDIR "/src/Security.Api"
RUN dotnet build "Security.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Security.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Security.Api.dll"]
```

### Azure Container Apps Configuration

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: security-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: security-service
  template:
    metadata:
      labels:
        app: security-service
    spec:
      containers:
        - name: security-service
          image: bankregistry.azurecr.io/security-service:latest
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: security-secrets
                  key: database-connection
```

## 🔧 Development Setup

### Prerequisites

- .NET 9 SDK
- SQL Server or SQL Server Express
- Visual Studio 2022 or VS Code

### Local Development

1. **Update connection string** in `appsettings.Development.json`
2. **Run database migrations**:
   ```bash
   dotnet ef database update --project Security.Infrastructure --startup-project Security.Api
   ```
3. **Start the service**:
   ```bash
   dotnet run --project Security.Api
   ```
4. **Access Swagger UI**: `https://localhost:5001/swagger`

### Database Migrations

**Package Configuration**: The `Microsoft.EntityFrameworkCore.Design` package is configured only in the **Security.Api** project to avoid conflicts.

**All commands should be run from the `src/services/Security/src/` directory.**

```bash
# Add new migration
dotnet ef migrations add <MigrationName> --project Security.Infrastructure --startup-project Security.Api

# Update database
dotnet ef database update --project Security.Infrastructure --startup-project Security.Api

# List all migrations
dotnet ef migrations list --project Security.Infrastructure --startup-project Security.Api

# Remove last migration (only if not applied to database)
dotnet ef migrations remove --project Security.Infrastructure --startup-project Security.Api

# Generate SQL script for production deployment
dotnet ef migrations script --project Security.Infrastructure --startup-project Security.Api --output migration-script.sql

# Apply specific migration
dotnet ef database update <MigrationName> --project Security.Infrastructure --startup-project Security.Api
```

For more detailed Entity Framework documentation, see [Entity Framework Guidelines](../../docs/guidelines/entity-framework.md).

## 🤝 Contributing

1. Follow the [Clean Architecture principles](../../docs/dotnet-development-guidelines.md)
2. Implement comprehensive unit tests
3. Follow security best practices
4. Update API documentation
5. Follow conventional commit messages

## 📚 Additional Resources

- [JWT Best Practices](https://tools.ietf.org/html/rfc7519)
- [ASP.NET Core Identity](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [OWASP Authentication Guidelines](https://owasp.org/www-project-cheat-sheets/cheatsheets/Authentication_Cheat_Sheet.html)
- [Azure AD B2C Integration](https://docs.microsoft.com/en-us/azure/active-directory-b2c/)
