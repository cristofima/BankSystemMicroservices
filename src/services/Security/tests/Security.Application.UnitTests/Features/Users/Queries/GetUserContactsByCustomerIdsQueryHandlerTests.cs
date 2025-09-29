using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Security.Application.Features.Users.Queries;
using Security.Application.Interfaces;
using Security.Application.UnitTests.Common;
using Security.Domain.Entities;

namespace Security.Application.UnitTests.Features.Users.Queries;

/// <summary>
/// Unit tests for GetUserContactsByCustomerIdsQueryHandler
/// </summary>
public class GetUserContactsByCustomerIdsQueryHandlerTests : TestBase
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<GetUserContactsByCustomerIdsQueryHandler>> _mockLogger;
    private readonly GetUserContactsByCustomerIdsQueryHandler _handler;

    public GetUserContactsByCustomerIdsQueryHandlerTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = CreateMockLogger<GetUserContactsByCustomerIdsQueryHandler>();
        _handler = new GetUserContactsByCustomerIdsQueryHandler(
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
        var act = () => new GetUserContactsByCustomerIdsQueryHandler(null!, _mockLogger.Object);
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
            new GetUserContactsByCustomerIdsQueryHandler(_mockUserRepository.Object, null!);
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("logger");
    }

    /// <summary>
    /// Verifies handler returns success result with users when users are found
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUsersAreFound()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        var users = new List<ApplicationUser>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = customerIds[0],
                Email = "user1@example.com",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "+1234567890",
                LockoutEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = customerIds[1],
                Email = "user2@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "+0987654321",
                LockoutEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-20),
                UpdatedAt = DateTimeOffset.UtcNow,
            },
        };

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);

        var userContactDtos = result.Value!.ToList();
        userContactDtos[0].CustomerId.Should().Be(customerIds[0]);
        userContactDtos[0].Email.Should().Be("user1@example.com");
        userContactDtos[1].CustomerId.Should().Be(customerIds[1]);
        userContactDtos[1].Email.Should().Be("user2@example.com");
    }

    /// <summary>
    /// Verifies handler returns success with empty collection when empty list is provided
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccessWithEmptyCollection_WhenEmptyListProvided()
    {
        // Arrange
        var customerIds = new List<Guid>();
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();

        // Verify repository is not called
        _mockUserRepository.Verify(
            x =>
                x.GetUsersByCustomerIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// Verifies handler returns failure when batch size exceeds maximum
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenBatchSizeExceedsMaximum()
    {
        // Arrange
        var customerIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Batch size cannot exceed 100 items");
        result.Value.Should().BeNull();

        // Verify repository is not called
        _mockUserRepository.Verify(
            x =>
                x.GetUsersByCustomerIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    /// <summary>
    /// Verifies handler correctly maps locked out users as inactive
    /// </summary>
    [Fact]
    public async Task Handle_ShouldMapLockedOutUsersAsInactive()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        var users = new List<ApplicationUser>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = customerIds[0],
                Email = "locked@example.com",
                LockoutEnabled = true,
                LockoutEnd = DateTimeOffset.UtcNow.AddHours(1), // Locked out
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            },
        };

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().IsActive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies handler correctly maps expired lockout users as active
    /// </summary>
    [Fact]
    public async Task Handle_ShouldMapExpiredLockoutUsersAsActive()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        var users = new List<ApplicationUser>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = customerIds[0],
                Email = "expired@example.com",
                LockoutEnabled = true,
                LockoutEnd = DateTimeOffset.UtcNow.AddHours(-1), // Lockout expired
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            },
        };

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.First().IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies handler handles null user properties gracefully
    /// </summary>
    [Fact]
    public async Task Handle_ShouldHandleNullPropertiesGracefully()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        var users = new List<ApplicationUser>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = customerIds[0],
                Email = null,
                FirstName = null,
                LastName = null,
                PhoneNumber = null,
                LockoutEnabled = false,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
                UpdatedAt = null,
            },
        };

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var userDto = result.Value!.First();
        userDto.Email.Should().Be(string.Empty);
        userDto.FirstName.Should().Be(string.Empty);
        userDto.LastName.Should().Be(string.Empty);
        userDto.PhoneNumber.Should().Be(string.Empty);
        userDto.UpdatedAt.Should().BeNull();
    }

    /// <summary>
    /// Verifies handler returns success with empty collection when no users found
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnSuccessWithEmptyCollection_WhenNoUsersFound()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies handler returns failure when repository throws exception
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenRepositoryThrowsException()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
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
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, cancellationToken))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockUserRepository.Verify(
            x => x.GetUsersByCustomerIdsAsync(customerIds, cancellationToken),
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
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        var users = new List<ApplicationUser>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                ClientId = customerIds[0],
                Email = "test@example.com",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-30),
            },
        };

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

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
                            v.ToString()!.Contains("Processing GetUserContactsByCustomerIdsQuery")
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
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully retrieved")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Verifies handler logs warning for empty customer IDs list
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogWarning_ForEmptyCustomerIdsList()
    {
        // Arrange
        var customerIds = new List<Guid>();
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) => v.ToString()!.Contains("Empty customer IDs list provided")
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Verifies handler logs warning for batch size exceeding maximum
    /// </summary>
    [Fact]
    public async Task Handle_ShouldLogWarning_ForExcessiveBatchSize()
    {
        // Arrange
        var customerIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x =>
                x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (v, t) =>
                            v.ToString()!.Contains("Batch size")
                            && v.ToString()!.Contains("exceeds maximum")
                    ),
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
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);
        var exception = new InvalidOperationException("Database error");

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
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
                        (v, t) =>
                            v.ToString()!
                                .Contains("Error retrieving user contacts for batch request")
                    ),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Once
        );
    }

    /// <summary>
    /// Verifies handler handles maximum batch size exactly at limit
    /// </summary>
    [Fact]
    public async Task Handle_ShouldProcessSuccessfully_WhenBatchSizeIsExactlyAtLimit()
    {
        // Arrange
        var customerIds = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();
        var query = new GetUserContactsByCustomerIdsQuery(customerIds);

        _mockUserRepository
            .Setup(x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ApplicationUser>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        _mockUserRepository.Verify(
            x => x.GetUsersByCustomerIdsAsync(customerIds, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
