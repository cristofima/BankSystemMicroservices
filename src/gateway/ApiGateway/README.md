# API Gateway

## Overview

The API Gateway is the central entry point for the Bank System Microservices architecture. It implements a **hybrid authentication** approach with the following core features:

- ✅ **Rate limiting handled at API Gateway level**
- ✅ **YARP Reverse Proxy** for routing to downstream services
- ✅ **Hybrid Authentication** with selective endpoint protection
- ✅ **Health Monitoring** for service availability
- ✅ **Centralized Logging** with correlation tracking
- ✅ **Security Headers** and middleware stack

## Architecture

### Core Components

1. **YARP Reverse Proxy**

   - Routes requests to downstream microservices
   - Load balancing and failover capabilities
   - Service discovery integration

2. **Authentication & Authorization**

   - JWT-based authentication with selective endpoint protection
   - Role-based access control (RBAC)
   - Public endpoints for authentication flows

3. **Rate Limiting**

   - API throttling to prevent abuse
   - Configurable limits per endpoint/user
   - Protection against DDoS attacks

4. **Health Monitoring**

   - Health checks for downstream services
   - Service availability reporting
   - Health check UI dashboard

5. **Logging & Monitoring**

   - Centralized request/response logging
   - Correlation ID tracking across services
   - Structured logging with Serilog

6. **Security**
   - Security headers middleware
   - CORS configuration
   - Exception handling with secure error responses
   - Centralized error handling with RFC 7807 Problem Details (application/problem+json)
   - JWT authentication with proper 401 (RFC 7235 §3.1) challenge semantics

### Service Routing

The gateway routes requests to the following downstream services:

- **Security Service**: `localhost:5153` → Routes: `/security/*`
- **Account Service**: `localhost:5069` → Routes: `/accounts/*`
- **Movement Service**: `localhost:5071` → Routes: `/movements/*`
- **Transaction Service**: `localhost:5073` → Routes: `/transactions/*`

## Authentication Flow

### Public Endpoints (No Authentication Required)

These endpoints are accessible without JWT tokens:

```bash
# Authentication endpoints (routed to Security service)
POST /api/v1/auth/login
POST /api/v1/auth/register
POST /api/v1/auth/refresh
POST /api/v1/auth/forgot-password
POST /api/v1/auth/reset-password

# Gateway system endpoints
GET /health
GET /health/ready
GET /health/live
```

### Protected Endpoints (Authentication Required)

All other endpoints require valid JWT tokens in the Authorization header:

```bash
# Account operations (routed to Account service)
GET /accounts
POST /accounts
GET /accounts/{id}

# Transaction operations (routed to Transaction service)
GET /transactions
POST /transactions

# Movement operations (routed to Movement service)
GET /movements
POST /movements
```

## Testing the Authentication

### 1. Test Public Endpoint (No Token Required)

```bash
# Test authentication endpoint (routed to Security service)
curl -X POST https://localhost:55836/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "password123"
  }'

# Test health check
curl -X GET https://localhost:55836/health
```

### 2. Test Protected Endpoint (Token Required)

```bash
# This should return 401 Unauthorized without a token
curl -X GET https://localhost:55836/api/v1/accounts

# Expected 401 response with Problem Details JSON:
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required to access this resource.",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2025-01-28T..."
}

# This should work with a valid token
curl -X GET https://localhost:55836/api/v1/accounts \
  -H "Authorization: Bearer your-jwt-token-here"
```

## JWT Token Configuration

Your JWT configuration is in `appsettings.Development.json`:

```json
"Authentication": {
  "Jwt": {
    "Key": "<YOUR_JWT_SECRET_KEY>",
    "Issuer": "https://localhost:55836",
    "Audience": "bank-system-api",
    "AccessTokenExpiryInMinutes": 15,
    "RefreshTokenExpiryInDays": 7
  }
}
```

## Authorization Policies

The system implements several authorization policies:

### 1. PublicEndpoints

- **Access**: Anyone (no authentication required)
- **Endpoints**: Login, register, health checks

### 2. AuthenticatedUsers

- **Access**: Any authenticated user
- **Requirement**: Valid JWT token

### 3. AdminOnly

- **Access**: Users with "Admin" role
- **Requirement**: JWT token + "role" claim = "Admin"

### 4. ManagerOrAdmin

- **Access**: Users with "Manager" or "Admin" roles
- **Requirement**: JWT token + appropriate role claim

### 5. AccountOwnerOrAdmin

- **Access**: Account owners or administrators
- **Requirement**: JWT token + (owns resource OR admin role)

## Project Structure

```
ApiGateway/
├── Configuration/          # Configuration models and options
│   ├── GatewayOptions.cs   # Gateway-specific configuration
│   └── YarpOptions.cs      # YARP reverse proxy configuration
├── Extensions/             # Service registration and pipeline setup
│   └── ServiceExtensions.cs
├── Middleware/             # Custom middleware components
│   ├── CorrelationIdMiddleware.cs
│   └── ExceptionHandlingMiddleware.cs
├── appsettings.json        # Base configuration
├── appsettings.Development.json  # Development-specific settings
├── Program.cs             # Application entry point
└── README.md             # This documentation
```

## Configuration

### appsettings.json Structure

```json
{
  "Authentication": {
    "Jwt": {
      "Key": "your-super-secret-jwt-key-for-development-that-is-at-least-32-characters-long",
      "Issuer": "https://localhost:55836",
      "Audience": "bank-system-api",
      "AccessTokenExpiryInMinutes": 15,
      "RefreshTokenExpiryInDays": 7
    }
  },
  "ReverseProxy": {
    "Routes": {
      "security-route": {
        "ClusterId": "security-cluster",
        "Match": {
          "Path": "/security/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      },
      "accounts-route": {
        "ClusterId": "accounts-cluster",
        "Match": {
          "Path": "/accounts/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      },
      "movements-route": {
        "ClusterId": "movements-cluster",
        "Match": {
          "Path": "/movements/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      },
      "transactions-route": {
        "ClusterId": "transactions-cluster",
        "Match": {
          "Path": "/transactions/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      }
    },
    "Clusters": {
      "security-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:5153/"
          }
        }
      },
      "accounts-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:5069/"
          }
        }
      },
      "movements-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:5071/"
          }
        }
      },
      "transactions-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "https://localhost:5073/"
          }
        }
      }
    }
  },
  "HealthChecks": {
    "Services": [
      {
        "Name": "Security Service",
        "Url": "https://localhost:5153/health"
      },
      {
        "Name": "Account Service",
        "Url": "https://localhost:5069/health"
      },
      {
        "Name": "Movement Service",
        "Url": "https://localhost:5071/health"
      },
      {
        "Name": "Transaction Service",
        "Url": "https://localhost:5073/health"
      }
    ]
  },
  "RateLimiting": {
    "AuthPolicy": {
      "PermitLimit": 10,
      "Window": "00:01:00"
    },
    "ApiPolicy": {
      "PermitLimit": 100,
      "Window": "00:01:00"
    }
  }
}
```

### Key Configuration Sections

- **YARP Routes**: Define URL patterns and target services with path transforms
- **JWT Settings**: Authentication configuration matching downstream services
- **Rate Limiting**: API throttling policies per endpoint type
- **Health Checks**: Service monitoring configuration for all microservices

## Error Handling

### Centralized Exception Handling

The gateway implements centralized error handling using the `ExceptionHandlingMiddleware` which:

- **Catches All Unhandled Exceptions**: Provides consistent error responses across all routes
- **RFC 7807 Problem Details**: Returns standardized error responses with proper HTTP status codes
- **Correlation ID Tracking**: Includes correlation IDs in all error responses for request tracing
- **Security Considerations**: Prevents sensitive information exposure in production

### Error Response Format

All errors return RFC 7807 Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authentication is required to access this resource",
  "instance": "POST /api/v1/accounts",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### JWT Authentication Error Handling

The gateway provides enhanced JWT authentication with proper error responses:

- **401 Unauthorized**: Invalid, expired, or missing JWT tokens
- **403 Forbidden**: Valid JWT token but insufficient permissions for the resource
- **JSON Error Responses**: All authentication failures return structured JSON instead of default HTML responses

### Middleware Pipeline Order

Critical middleware ordering for proper error handling:

1. **CorrelationIdMiddleware**: Adds correlation ID to requests
2. **ExceptionHandlingMiddleware**: Catches all exceptions (must be before authentication)
3. **Authentication**: JWT token validation
4. **Authorization**: Permission checks
5. **YARP Reverse Proxy**: Routes to downstream services

## Monitoring

### Health Checks

- **Gateway Health**: `https://localhost:55836/health`
- **Service Health**: Individual service health tracked and aggregated
- **Health Check UI**: Available at `/healthchecks-ui` (if enabled)

### Logging

The gateway uses structured logging with correlation IDs:

```json
{
  "Timestamp": "2024-01-15T10:30:00.000Z",
  "Level": "Information",
  "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
  "Message": "Request processed",
  "Properties": {
    "Method": "GET",
    "Path": "/accounts",
    "StatusCode": 200,
    "Duration": 150
  }
}
```

### Metrics

Key metrics tracked:

- Request throughput per service
- Response times and latency
- Error rates and status codes
- Rate limiting hits and blocks
- Authentication success/failure rates
- Health check status changes

## Troubleshooting

### Common Issues

1. **401 Unauthorized on Public Endpoints**

   - Check route patterns in authentication middleware
   - Verify public endpoints are properly excluded from authentication
   - Ensure `/security/auth/*` paths are marked as public

2. **YARP Routing Issues**

   - Verify downstream services are running on configured ports
   - Check cluster configuration in appsettings.json
   - Ensure SSL certificates are valid for all services
   - Test direct service endpoints before routing through gateway

3. **Rate Limiting Too Aggressive**

   - Adjust permit limits in configuration
   - Review time windows for rate limiting policies
   - Check if rate limiting applies to correct endpoint patterns

4. **JWT Token Issues**

   - Ensure issuer URLs match between gateway and services
   - Verify JWT key is consistent across all services
   - Check token expiration settings and clock skew
   - Validate audience claims match expected values

5. **Service Discovery Problems**
   - Confirm all downstream services are healthy
   - Check network connectivity between gateway and services
   - Verify service ports match configuration
   - Review firewall and security group settings

### Debugging Tips

1. **Enable Detailed Logging**:

   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Information",
       "Microsoft.AspNetCore.HttpLogging": "Information",
       "Yarp": "Debug"
     }
   }
   ```

2. **Test Service Connectivity**:

   ```bash
   # Test direct service health
   curl http://localhost:5153/health
   curl http://localhost:5069/health

   # Test through gateway
   curl https://localhost:55836/health
   ```

3. **Verify Authentication Flow**:

   ```bash
   # Get token from security service
   curl -X POST https://localhost:55836/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"password123"}'

   # Use token with protected endpoint
   curl https://localhost:55836/api/v1/accounts \
     -H "Authorization: Bearer [TOKEN]" \
     -v
   ```

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- Valid SSL certificates for HTTPS (development certificates work)
- Downstream services running on configured ports

### Running the Gateway

1. **Development Mode**:

   ```bash
   cd src/gateway/ApiGateway
   dotnet run
   ```

2. **With specific environment**:

   ```bash
   dotnet run --environment Development
   ```

3. **Using VS Code tasks**:
   - Open Command Palette (`Ctrl+Shift+P`)
   - Run task: "run-api-gateway" (if configured)

### Endpoints

Once running, the gateway will be available at:

- **Main URL**: `https://localhost:55836`
- **Health Checks**: `https://localhost:55836/health`
- **Security Service**: `https://localhost:55836/security/*`
- **Account Service**: `https://localhost:55836/accounts/*`

### Testing

1. **Health Check**:

   ```bash
   curl https://localhost:55836/health
   ```

2. **Authentication (Public)**:

   ```bash
   curl -X POST https://localhost:55836/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"password123"}'
   ```

3. **Protected Endpoint**:

   ```bash
   curl https://localhost:55836/api/v1/accounts \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

4. **Authentication Error Response (401)**:

   ```bash
   # Request without token
   curl https://localhost:55836/api/v1/accounts

   # Response:
   {
     "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
     "title": "Unauthorized",
     "status": 401,
     "detail": "Authentication is required to access this resource",
     "instance": "POST /api/v1/accounts",
     "correlationId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
     "timestamp": "2024-01-15T14:30:00.000Z"
   }
   ```

5. **Invalid Token Error Response (401)**:

   ```bash
   # Request with invalid token
   curl https://localhost:55836/api/v1/accounts \
     -H "Authorization: Bearer <INVALID_TOKEN>"

   # Response:
   {
     "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
     "title": "Unauthorized",
     "status": 401,
     "detail": "Authentication is required to access this resource",
     "instance": "POST /api/v1/accounts",
     "correlationId": "550e8400-e29b-41d4-a716-446655440000",
     "timestamp": "2024-01-15T14:31:15.000Z"
   }
   ```
