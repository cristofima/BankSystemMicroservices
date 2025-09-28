using Security.Application.Interfaces;
using Security.Infrastructure.IntegrationTests.Common;

namespace Security.Infrastructure.IntegrationTests.Repositories;

/// <summary>
/// Integration tests for UserRepository
/// </summary>
public class UserRepositoryTests : BaseSecurityInfrastructureTest
{
    /// <summary>
    /// Gets the UserRepository instance from the service provider
    /// </summary>
    private IUserRepository GetUserRepository()
    {
        return GetService<IUserRepository>();
    }

    /// <summary>
    /// Creates a test user with the specified parameters
    /// </summary>
    private async Task<ApplicationUser> CreateTestUserAsync(
        string? email = null,
        string? userName = null,
        Guid? clientId = null
    )
    {
        var uniqueId = Guid.NewGuid();
        email ??= $"testuser-{uniqueId:N}@test.com";
        userName ??= $"testuser-{uniqueId:N}"[..30];
        clientId ??= uniqueId;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            Email = email,
            NormalizedUserName = userName.ToUpperInvariant(),
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            ClientId = clientId.Value,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+1234567890",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var context = GetDbContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    #region GetUserByCustomerIdAsync Tests

    /// <summary>
    /// Verifies GetUserByCustomerIdAsync returns user when customer ID exists
    /// </summary>
    [Fact]
    public async Task GetUserByCustomerIdAsync_ShouldReturnUser_WhenCustomerIdExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var createdUser = await CreateTestUserAsync(clientId: customerId);
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUserByCustomerIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdUser.Id);
        result.ClientId.Should().Be(customerId);
        result.Email.Should().Be(createdUser.Email);
        result.FirstName.Should().Be(createdUser.FirstName);
        result.LastName.Should().Be(createdUser.LastName);
        result.PhoneNumber.Should().Be(createdUser.PhoneNumber);
    }

    /// <summary>
    /// Verifies GetUserByCustomerIdAsync returns null when customer ID does not exist
    /// </summary>
    [Fact]
    public async Task GetUserByCustomerIdAsync_ShouldReturnNull_WhenCustomerIdDoesNotExist()
    {
        // Arrange
        var nonExistentCustomerId = Guid.NewGuid();
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUserByCustomerIdAsync(nonExistentCustomerId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies GetUserByCustomerIdAsync works with cancellation token
    /// </summary>
    [Fact]
    public async Task GetUserByCustomerIdAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        await CreateTestUserAsync(clientId: customerId);
        var repository = GetUserRepository();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.GetUserByCustomerIdAsync(customerId, cts.Token)
        );
    }

    /// <summary>
    /// Verifies GetUserByCustomerIdAsync returns correct user when multiple users exist
    /// </summary>
    [Fact]
    public async Task GetUserByCustomerIdAsync_ShouldReturnCorrectUser_WhenMultipleUsersExist()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var customerId3 = Guid.NewGuid();

        var user1 = await CreateTestUserAsync("user1@test.com", "user1", customerId1);
        await CreateTestUserAsync("user2@test.com", "user2", customerId2);
        await CreateTestUserAsync("user3@test.com", "user3", customerId3);

        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUserByCustomerIdAsync(customerId1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user1.Id);
        result.ClientId.Should().Be(customerId1);
        result.Email.Should().Be("user1@test.com");
    }

    /// <summary>
    /// Verifies GetUserByCustomerIdAsync handles empty GUID
    /// </summary>
    [Fact]
    public async Task GetUserByCustomerIdAsync_ShouldReturnNull_WhenCustomerIdIsEmptyGuid()
    {
        // Arrange
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUserByCustomerIdAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Verifies GetUserByCustomerIdAsync uses AsNoTracking for read-only operations
    /// </summary>
    [Fact]
    public async Task GetUserByCustomerIdAsync_ShouldUseNoTracking()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        await CreateTestUserAsync(clientId: customerId);
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUserByCustomerIdAsync(customerId);

        // Assert - User should be returned but not tracked by EF
        result.Should().NotBeNull();

        // Verify the entity is not being tracked
        var context = GetDbContext();
        var tracked = context.Entry(result!).State;
        tracked.Should().Be(Microsoft.EntityFrameworkCore.EntityState.Detached);
    }

    #endregion GetUserByCustomerIdAsync Tests

    #region GetUsersByCustomerIdsAsync Tests

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync returns users for existing customer IDs
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldReturnUsers_WhenCustomerIdsExist()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var customerId3 = Guid.NewGuid();

        var user1 = await CreateTestUserAsync("user1@test.com", "user1", customerId1);
        var user2 = await CreateTestUserAsync("user2@test.com", "user2", customerId2);
        await CreateTestUserAsync("user3@test.com", "user3", customerId3);

        var customerIds = new List<Guid> { customerId1, customerId2 };
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);

        var usersList = result.ToList();
        usersList.Should().Contain(u => u.Id == user1.Id && u.ClientId == customerId1);
        usersList.Should().Contain(u => u.Id == user2.Id && u.ClientId == customerId2);
        usersList.Should().NotContain(u => u.ClientId == customerId3);
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync returns empty collection when no customer IDs provided
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldReturnEmptyCollection_WhenNoCustomerIdsProvided()
    {
        // Arrange
        var customerIds = new List<Guid>();
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync returns empty collection when customer IDs do not exist
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldReturnEmptyCollection_WhenCustomerIdsDoNotExist()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync returns partial results when some customer IDs exist
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldReturnPartialResults_WhenSomeCustomerIdsExist()
    {
        // Arrange
        var existingCustomerId = Guid.NewGuid();
        var nonExistentCustomerId = Guid.NewGuid();

        var existingUser = await CreateTestUserAsync(clientId: existingCustomerId);
        var customerIds = new List<Guid> { existingCustomerId, nonExistentCustomerId };
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(existingUser.Id);
        result.First().ClientId.Should().Be(existingCustomerId);
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync handles single customer ID
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldWork_WithSingleCustomerId()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var user = await CreateTestUserAsync(clientId: customerId);
        var customerIds = new List<Guid> { customerId };
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Id.Should().Be(user.Id);
        result.First().ClientId.Should().Be(customerId);
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync handles maximum batch size (100)
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldWork_WithMaximumBatchSize()
    {
        // Arrange - Create exactly 100 users
        var customerIds = new List<Guid>();
        var createdUsers = new List<ApplicationUser>();

        for (var i = 0; i < 100; i++)
        {
            var customerId = Guid.NewGuid();
            var user = await CreateTestUserAsync($"user{i}@test.com", $"user{i}", customerId);
            customerIds.Add(customerId);
            createdUsers.Add(user);
        }

        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        var applicationUsers = result as ApplicationUser[] ?? result.ToArray();
        applicationUsers.Should().NotBeNull();
        applicationUsers.Should().HaveCount(100);

        var resultList = applicationUsers.ToList();
        foreach (var createdUser in createdUsers)
        {
            resultList
                .Should()
                .Contain(u => u.Id == createdUser.Id && u.ClientId == createdUser.ClientId);
        }
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync throws exception when batch size exceeds maximum
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldThrowException_WhenBatchSizeExceedsMaximum()
    {
        // Arrange - Create 101 customer IDs (exceeds limit)
        var customerIds = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var repository = GetUserRepository();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetUsersByCustomerIdsAsync(customerIds)
        );

        exception.Message.Should().Contain("Batch size cannot exceed 100 items");
        exception.ParamName.Should().Be("customerIds");
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync respects cancellation token
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var customerIds = new List<Guid> { Guid.NewGuid() };
        var repository = GetUserRepository();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => repository.GetUsersByCustomerIdsAsync(customerIds, cts.Token)
        );
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync handles duplicate customer IDs
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldHandleDuplicateCustomerIds()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var user = await CreateTestUserAsync(clientId: customerId);
        var customerIds = new List<Guid> { customerId, customerId, customerId }; // Duplicates
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1); // Should return only one user despite duplicates
        result.First().Id.Should().Be(user.Id);
        result.First().ClientId.Should().Be(customerId);
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync uses AsNoTracking for read-only operations
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldUseNoTracking()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        await CreateTestUserAsync(clientId: customerId);
        var customerIds = new List<Guid> { customerId };
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        // Verify the entities are not being tracked
        var context = GetDbContext();
        var user = result.First();
        var tracked = context.Entry(user).State;
        tracked.Should().Be(Microsoft.EntityFrameworkCore.EntityState.Detached);
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync returns users with all properties populated
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldReturnUsersWithAllProperties()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var createdUser = await CreateTestUserAsync(
            email: "complete@test.com",
            userName: "completeuser",
            clientId: customerId
        );

        var customerIds = new List<Guid> { customerId };
        var repository = GetUserRepository();

        // Act
        var result = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        var applicationUsers = result as ApplicationUser[] ?? result.ToArray();
        applicationUsers.Should().NotBeNull();
        applicationUsers.Should().HaveCount(1);

        var user = applicationUsers.First();
        user.Id.Should().Be(createdUser.Id);
        user.ClientId.Should().Be(customerId);
        user.Email.Should().Be("complete@test.com");
        user.UserName.Should().Be("completeuser");
        user.FirstName.Should().Be("Test");
        user.LastName.Should().Be("User");
        user.PhoneNumber.Should().Be("+1234567890");
        user.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        user.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Verifies GetUsersByCustomerIdsAsync preserves order based on database retrieval
    /// </summary>
    [Fact]
    public async Task GetUsersByCustomerIdsAsync_ShouldReturnUsersInConsistentOrder()
    {
        // Arrange
        var customerId1 = Guid.NewGuid();
        var customerId2 = Guid.NewGuid();
        var customerId3 = Guid.NewGuid();

        await CreateTestUserAsync("user1@test.com", "user1", customerId1);
        await CreateTestUserAsync("user2@test.com", "user2", customerId2);
        await CreateTestUserAsync("user3@test.com", "user3", customerId3);

        var customerIds = new List<Guid> { customerId3, customerId1, customerId2 }; // Different order
        var repository = GetUserRepository();

        // Act - Run multiple times to verify consistency
        var result1 = await repository.GetUsersByCustomerIdsAsync(customerIds);
        var result2 = await repository.GetUsersByCustomerIdsAsync(customerIds);

        // Assert
        result1.Should().HaveCount(3);
        result2.Should().HaveCount(3);

        var users1 = result1.ToList();
        var users2 = result2.ToList();

        // Results should be consistent across calls
        for (var i = 0; i < users1.Count; i++)
        {
            users1[i].Id.Should().Be(users2[i].Id);
        }
    }

    #endregion GetUsersByCustomerIdsAsync Tests
}
