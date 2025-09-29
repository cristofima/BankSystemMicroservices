using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Security.Application.Features.Users.Queries;
using Security.Application.Interfaces;
using Security.Application.UnitTests.Common;
using Security.Domain.Entities;

namespace Security.Application.UnitTests.Features.Users.Queries;

/// <summary>
/// Unit tests for GetUserContactByCustomerIdQueryHandler
/// </summary>
public class GetUserContactByCustomerIdQueryHandlerTests : TestBase
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<GetUserContactByCustomerIdQueryHandler>> _mockLogger;
    private readonly GetUserContactByCustomerIdQueryHandler _handler;

    public GetUserContactByCustomerIdQueryHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = CreateMockLogger<GetUserContactByCustomerIdQueryHandler>();
        _handler = new GetUserContactByCustomerIdQueryHandler(
            _mockUserRepository.Object,
            _mockLogger.Object
        );
    }

    /// <summary>
    /// Verifies constructor throws ArgumentNullException for null userRepository
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserRepositoryIsNull()
    {
        // Act & Assert
        var act = () => new GetUserContactByCustomerIdQueryHandler(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("userRepository");
    }

    /// <summary>
    /// Verifies constructor throws ArgumentNullException for null logger
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var act = () =>
            new GetUserContactByCustomerIdQueryHandler(_mockUserRepository.Object, null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    /// <summary>
    /// Verifies handler returns success result when user is found
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserIsFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = customerId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            LockoutEnabled = false,
            LockoutEnd = null,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CustomerId.Should().Be(customerId);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.FirstName.Should().Be("John");
        result.Value.LastName.Should().Be("Doe");
        result.Value.PhoneNumber.Should().Be("+1234567890");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAt.Should().Be(user.CreatedAt.DateTime);
        result.Value.UpdatedAt.Should().Be(user.UpdatedAt!.Value.DateTime);
    }

    /// <summary>
    /// Verifies handler returns failure result when user is not found
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Value.Should().BeNull();
    }

    /// <summary>
    /// Verifies handler correctly maps locked out user as inactive
    /// </summary>
    [Fact]
    public async Task Handle_ShouldMapLockedOutUserAsInactive()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = customerId,
            Email = "test@example.com",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(1), // Locked out
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
        };

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies handler correctly maps expired lockout user as active
    /// </summary>
    [Fact]
    public async Task Handle_ShouldMapExpiredLockoutUserAsActive()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = customerId,
            Email = "test@example.com",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddHours(-1), // Lockout expired
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
        };

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies handler handles null user properties gracefully
    /// </summary>
    [Fact]
    public async Task Handle_ShouldHandleNullPropertiesGracefully()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = customerId,
            Email = null,
            FirstName = null,
            LastName = null,
            PhoneNumber = null,
            LockoutEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            UpdatedAt = null,
        };

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(string.Empty);
        result.Value.FirstName.Should().Be(string.Empty);
        result.Value.LastName.Should().Be(string.Empty);
        result.Value.PhoneNumber.Should().Be(string.Empty);
        result.Value.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// Verifies handler returns failure when repository throws exception
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("An error occurred while retrieving user contact information");
        result.Value.Should().BeNull();
    }

    /// <summary>
    /// Verifies handler passes cancellation token to repository
    /// </summary>
    [Fact]
    public async Task Handle_ShouldPassCancellationToken_ToRepository()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, cancellationToken))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockUserRepository.Verify(
            x => x.GetUserByCustomerIdAsync(customerId, cancellationToken),
            Times.Once
        );
    }

    /// <summary>
    /// Verifies handler logs debug messages for successful processing
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogDebugMessages_ForSuccessfulProcessing()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            ClientId = customerId,
            Email = "test@example.com",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
        };

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("Processing GetUserContactByCustomerIdQuery")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );

        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Successfully retrieved user contact")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Verifies handler logs warning when user is not found
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogWarning_WhenUserNotFound()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Verifies handler logs error when exception occurs
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var query = new GetUserContactByCustomerIdQuery(customerId);
        var exception = new InvalidOperationException("Database error");

        _mockUserRepository
            .Setup(x => x.GetUserByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Error retrieving user contact")
                    ),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }
}
