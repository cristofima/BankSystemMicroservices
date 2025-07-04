using Security.Infrastructure.Data;
using Security.Infrastructure.IntegrationTests.Common;

namespace Security.Infrastructure.IntegrationTests.Simplified;

/// <summary>
/// Simplified integration tests for the Security.Infrastructure project
/// These tests focus on the basic database operations and are designed to compile and run successfully
/// </summary>
[TestFixture]
public class SimplifiedIntegrationTests : SecurityTestBase
{
    [Test]
    public async Task Database_ShouldCreateTables_WhenMigrated()
    {
        // Arrange & Act
        await DbContext.Database.EnsureCreatedAsync();

        // Assert
        var hasUsersTable = await DbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) as Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers'")
            .FirstOrDefaultAsync();
        
        hasUsersTable.Should().Be(1, "AspNetUsers table should exist");

        var hasRefreshTokensTable = await DbContext.Database
            .SqlQuery<int>($"SELECT COUNT(*) as Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens'")
            .FirstOrDefaultAsync();
        
        hasRefreshTokensTable.Should().Be(1, "RefreshTokens table should exist");
    }

    [Test]
    public async Task ApplicationUser_CanBeCreatedAndRetrieved()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserName = "testuser@example.com",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+1234567890",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Assert
        var retrievedUser = await DbContext.Users
            .FirstOrDefaultAsync(u => u.Email == "testuser@example.com");

        retrievedUser.Should().NotBeNull();
        retrievedUser!.UserName.Should().Be("testuser@example.com");
        retrievedUser.FirstName.Should().Be("Test");
        retrievedUser.LastName.Should().Be("User");
    }

    [Test]
    public async Task RefreshToken_CanBeCreatedAndRetrieved()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            JwtId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedByIp = "192.168.1.1"
        };

        // Act
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        // Assert
        var retrievedToken = await DbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);

        retrievedToken.Should().NotBeNull();
        retrievedToken!.UserId.Should().Be(user.Id);
        retrievedToken.JwtId.Should().Be(refreshToken.JwtId);
        retrievedToken.IsRevoked.Should().BeFalse();
    }

    [Test]
    public async Task RefreshToken_Relationships_ShouldWork()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            UserId = user.Id,
            JwtId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Act
        DbContext.RefreshTokens.Add(refreshToken);
        await DbContext.SaveChangesAsync();

        // Assert - Test cascade delete
        DbContext.Users.Remove(user);
        await DbContext.SaveChangesAsync();

        var tokenAfterUserDelete = await DbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);

        tokenAfterUserDelete.Should().BeNull("Refresh token should be deleted when user is deleted");
    }

    [Test]
    public async Task Database_Indexes_ShouldBeCreated()
    {
        // Arrange & Act
        await DbContext.Database.EnsureCreatedAsync();

        // Assert - Check if indexes exist (SQL Server specific)
        var indexCount = await DbContext.Database
            .SqlQuery<int>($@"
                SELECT COUNT(*) as Value 
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                WHERE t.name = 'RefreshTokens' 
                AND i.name IS NOT NULL")
            .FirstOrDefaultAsync();

        indexCount.Should().BeGreaterThan(0, "RefreshTokens table should have indexes");
    }

    [Test]
    public async Task ConcurrentOperations_ShouldWork()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var tasks = new List<Task>();

        // Act - Create multiple refresh tokens concurrently
        for (int i = 0; i < 5; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
                
                var token = new RefreshToken
                {
                    Token = $"token_{index}_{Guid.NewGuid()}",
                    UserId = user.Id,
                    JwtId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    IsRevoked = false
                };

                context.RefreshTokens.Add(token);
                await context.SaveChangesAsync();
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var userTokenCount = await DbContext.RefreshTokens
            .Where(rt => rt.UserId == user.Id)
            .CountAsync();

        userTokenCount.Should().Be(5, "All tokens should be created successfully");
    }

    [Test]
    public async Task DatabaseConnectionPool_ShouldWork()
    {
        // Arrange & Act
        var connectionTasks = new List<Task<bool>>();

        for (int i = 0; i < 10; i++)
        {
            connectionTasks.Add(Task.Run(async () =>
            {
                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
                
                // Simple query to test connection
                var userCount = await context.Users.CountAsync();
                return userCount >= 0; // Should always be true
            }));
        }

        var results = await Task.WhenAll(connectionTasks);

        // Assert
        results.Should().AllBeEquivalentTo(true, "All connection attempts should succeed");
    }

    private async Task<ApplicationUser> CreateTestUserAsync()
    {
        var uniqueId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            UserName = $"testuser_{uniqueId:N}@example.com",
            Email = $"testuser_{uniqueId:N}@example.com",
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+1234567890",
            ClientId = Guid.NewGuid() // Ensure unique ClientId
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }
}
