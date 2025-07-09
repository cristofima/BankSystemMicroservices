# Notification Service

## Overview

The Notification Service is responsible for managing all customer communications and notifications in the Bank System Microservices architecture. It handles multi-channel notifications including email, SMS, push notifications, and in-app messages.

## Architecture

This service follows Clean Architecture principles with clear separation of concerns:

```
Notification.Api/           # REST API controllers and configuration
Notification.Application/   # Business logic, handlers, and use cases
Notification.Domain/        # Domain entities and business rules
Notification.Infrastructure/ # Data access, external services, and messaging
```

## Responsibilities

### Core Functions

- **Email Notifications**: Send transactional and promotional emails
- **SMS Notifications**: Send text message alerts and confirmations
- **Push Notifications**: Mobile app push notifications
- **In-App Messages**: System messages within the banking application
- **Notification Templates**: Manage and render notification templates
- **Delivery Tracking**: Track notification delivery status and failures
- **User Preferences**: Manage customer notification preferences
- **Multi-Language Support**: Localized notifications based on user preferences

### What This Service DOES

- ✅ Manages notification templates and content
- ✅ Sends notifications via multiple channels (email, SMS, push, in-app)
- ✅ Tracks delivery status and handles failures
- ✅ Respects user notification preferences and opt-outs
- ✅ Provides notification analytics and reporting
- ✅ Handles notification scheduling and batching
- ✅ Manages communication compliance and regulations

### What This Service DOES NOT Do

- ❌ Does not store user account information
- ❌ Does not process transactions or payments
- ❌ Does not authenticate users (delegates to Security service)
- ❌ Does not generate business reports (delegates to Reporting service)
- ❌ Does not manage account balances or movements

## Domain Entities

### Core Entities

```csharp
public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
    public string Language { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class NotificationRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; }
    public NotificationType Type { get; set; }
    public string TemplateId { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public NotificationPriority Priority { get; set; }
    public DateTime ScheduledAt { get; set; }
    public NotificationStatus Status { get; set; }
    public List<DeliveryAttempt> DeliveryAttempts { get; set; }
}

public class UserNotificationPreferences
{
    public string UserId { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool InAppEnabled { get; set; }
    public List<string> OptedOutCategories { get; set; }
    public string PreferredLanguage { get; set; }
}
```

### Enums

```csharp
public enum NotificationType
{
    Email,
    Sms,
    Push,
    InApp
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Cancelled
}
```

## Events

### Events Published

```csharp
// When a notification is successfully sent
public record NotificationSentEvent(
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    DateTime SentAt
);

// When a notification fails to send
public record NotificationFailedEvent(
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    string ErrorMessage,
    int AttemptCount
);

// When a notification is delivered
public record NotificationDeliveredEvent(
    Guid NotificationId,
    string UserId,
    NotificationType Type,
    DateTime DeliveredAt
);
```

### Events Consumed

```csharp
// From Account Service
public record AccountCreatedEvent(Guid AccountId, string UserId, DateTime CreatedAt);
public record AccountStatusChangedEvent(Guid AccountId, string UserId, string NewStatus);

// From Transaction Service
public record TransactionCompletedEvent(Guid TransactionId, Guid AccountId, decimal Amount, DateTime CompletedAt);
public record TransactionFailedEvent(Guid TransactionId, Guid AccountId, string Reason);

// From Movement Service
public record TransferCompletedEvent(Guid TransferId, Guid FromAccountId, Guid ToAccountId, decimal Amount);
public record PaymentProcessedEvent(Guid PaymentId, Guid AccountId, decimal Amount, string PaymentType);

// From Security Service
public record UserRegisteredEvent(string UserId, string Email, DateTime RegisteredAt);
public record PasswordResetRequestedEvent(string UserId, string Email, string ResetToken);
public record SecurityAlertEvent(string UserId, string AlertType, string Description);
```

## API Endpoints

### Notification Management

```http
POST /api/notifications/send
POST /api/notifications/send-bulk
GET /api/notifications/{notificationId}
GET /api/notifications/user/{userId}
PUT /api/notifications/{notificationId}/retry
DELETE /api/notifications/{notificationId}
```

### Template Management

```http
GET /api/templates
POST /api/templates
GET /api/templates/{templateId}
PUT /api/templates/{templateId}
DELETE /api/templates/{templateId}
```

### User Preferences

```http
GET /api/preferences/{userId}
PUT /api/preferences/{userId}
POST /api/preferences/{userId}/opt-out/{category}
POST /api/preferences/{userId}/opt-in/{category}
```

### Analytics

```http
GET /api/analytics/delivery-rates
GET /api/analytics/user-engagement
GET /api/analytics/notification-volume
```

## Dependencies

### External Services

- **Email Provider**: SendGrid, AWS SES, or SMTP
- **SMS Provider**: Twilio, AWS SNS, or similar
- **Push Notification Provider**: Firebase, Azure Notification Hubs
- **Template Engine**: Razor, Handlebars, or similar

### Internal Dependencies

- **Security Service**: For user authentication and authorization
- **Account Service**: For account-related notifications
- **Transaction Service**: For transaction notifications
- **Movement Service**: For payment and transfer notifications

## Communication Patterns

### Synchronous Communication

- REST APIs for external clients
- HTTP calls to Security service for user validation

### Asynchronous Communication

- Event-driven architecture using Azure Service Bus
- Consumes events from other microservices
- Publishes notification events for analytics

## Configuration

### Key Settings

```json
{
  "NotificationProviders": {
    "Email": {
      "Provider": "SendGrid",
      "ApiKey": "your-sendgrid-api-key",
      "FromAddress": "noreply@yourbank.com",
      "FromName": "Your Bank"
    },
    "Sms": {
      "Provider": "Twilio",
      "AccountSid": "your-twilio-account-sid",
      "AuthToken": "your-twilio-auth-token",
      "FromNumber": "+1234567890"
    },
    "Push": {
      "Provider": "Firebase",
      "ServerKey": "your-firebase-server-key"
    }
  },
  "RetryPolicy": {
    "MaxAttempts": 3,
    "InitialDelaySeconds": 30,
    "BackoffMultiplier": 2
  },
  "BatchProcessing": {
    "BatchSize": 100,
    "ProcessingIntervalSeconds": 60
  }
}
```

## Testing

### Unit Testing Example (xUnit)

```csharp
public class NotificationServiceTests
{
    private readonly Mock<INotificationRepository> _mockRepository;
    private readonly Mock<IEmailProvider> _mockEmailProvider;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _mockRepository = new Mock<INotificationRepository>();
        _mockEmailProvider = new Mock<IEmailProvider>();
        _service = new NotificationService(_mockRepository.Object, _mockEmailProvider.Object);
    }

    [Fact]
    public async Task SendEmailNotification_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new SendNotificationRequest
        {
            UserId = "user123",
            Type = NotificationType.Email,
            TemplateId = "welcome-email",
            Parameters = new Dictionary<string, object> { { "UserName", "John Doe" } }
        };

        _mockEmailProvider.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRepository.Verify(x => x.SaveNotificationAsync(It.IsAny<NotificationRequest>()), Times.Once);
    }

    [Fact]
    public async Task SendEmailNotification_ProviderFails_ShouldReturnFailure()
    {
        // Arrange
        var request = new SendNotificationRequest
        {
            UserId = "user123",
            Type = NotificationType.Email,
            TemplateId = "welcome-email"
        };

        _mockEmailProvider.Setup(x => x.SendEmailAsync(It.IsAny<EmailMessage>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendNotificationAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Failed to send email", result.Error);
    }
}
```

### Integration Testing Example (xUnit)

```csharp
public class NotificationControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NotificationControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SendNotification_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            UserId = "user123",
            Type = "Email",
            TemplateId = "welcome-email",
            Parameters = new { UserName = "John Doe" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/notifications/send", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<NotificationResponse>(content);
        Assert.NotNull(result.NotificationId);
    }

    [Fact]
    public async Task GetUserPreferences_ValidUserId_ReturnsPreferences()
    {
        // Arrange
        var userId = "user123";

        // Act
        var response = await _client.GetAsync($"/api/preferences/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<UserNotificationPreferences>(content);
        Assert.Equal(userId, preferences.UserId);
    }
}
```

## Deployment

### Docker Configuration

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "Notification.Api.dll"]
```

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<NotificationDbContext>()
    .AddCheck<EmailProviderHealthCheck>("email-provider")
    .AddCheck<SmsProviderHealthCheck>("sms-provider")
    .AddCheck<ServiceBusHealthCheck>("service-bus");
```

## Monitoring and Logging

### Key Metrics

- **Notification Volume**: Number of notifications sent per type
- **Delivery Rate**: Percentage of successfully delivered notifications
- **Response Time**: Average time to process notification requests
- **Error Rate**: Percentage of failed notifications
- **Provider Performance**: Response times for external providers

### Logging Examples

```csharp
_logger.LogInformation("Notification {NotificationId} sent successfully to user {UserId} via {Type}",
    notification.Id, notification.UserId, notification.Type);

_logger.LogWarning("Failed to send notification {NotificationId} to user {UserId}. Attempt {Attempt} of {MaxAttempts}",
    notification.Id, notification.UserId, attempt, maxAttempts);

_logger.LogError(ex, "Error processing notification batch. BatchId: {BatchId}, Count: {Count}",
    batchId, notifications.Count);
```

## Performance Considerations

- **Batch Processing**: Process notifications in batches to improve throughput
- **Rate Limiting**: Respect provider rate limits to avoid throttling
- **Caching**: Cache templates and user preferences
- **Async Processing**: Use background workers for non-critical notifications
- **Circuit Breaker**: Implement circuit breaker pattern for external providers

## Security

- **Data Protection**: Encrypt sensitive notification content
- **Access Control**: Verify user permissions before sending notifications
- **Audit Trail**: Log all notification activities for compliance
- **Rate Limiting**: Prevent notification spam and abuse
- **Secure Configuration**: Store provider credentials securely

## Future Enhancements

- **Rich Content**: Support for HTML emails with attachments
- **A/B Testing**: Template testing and optimization
- **Intelligent Routing**: AI-powered optimal delivery time
- **Advanced Analytics**: Detailed engagement metrics
- **Webhook Support**: Real-time delivery status updates
- **Multi-Tenant**: Support for different bank brands
