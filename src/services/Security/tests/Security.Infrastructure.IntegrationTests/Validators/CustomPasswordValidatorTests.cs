using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Security.Infrastructure.Validators;

namespace Security.Infrastructure.IntegrationTests.Validators;

/// <summary>
/// Integration tests for CustomPasswordValidator to verify password validation rules
/// following OWASP security guidelines and bank system requirements
/// </summary>
public class CustomPasswordValidatorTests : IDisposable
{
    private CustomPasswordValidator _validator;
    private UserManager<ApplicationUser> _userManager;
    private ApplicationUser _testUser;

    public CustomPasswordValidatorTests()
    {
        // Create service collection for UserManager dependencies
        var services = new ServiceCollection();
        
        // Add minimal UserManager dependencies
        services.AddLogging();
        services.AddSingleton<IUserStore<ApplicationUser>, MockUserStore>();
        services.AddSingleton<IOptions<IdentityOptions>>(provider => 
            Options.Create(new IdentityOptions()));
        services.AddSingleton<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
        services.AddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
        services.AddSingleton<IdentityErrorDescriber>();
        services.AddSingleton<ILogger<UserManager<ApplicationUser>>>(provider =>
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<UserManager<ApplicationUser>>());

        var serviceProvider = services.BuildServiceProvider();
        
        // Create UserManager instance
        _userManager = new UserManager<ApplicationUser>(
            serviceProvider.GetRequiredService<IUserStore<ApplicationUser>>(),
            serviceProvider.GetRequiredService<IOptions<IdentityOptions>>(),
            serviceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            serviceProvider.GetRequiredService<ILookupNormalizer>(),
            serviceProvider.GetRequiredService<IdentityErrorDescriber>(),
            serviceProvider,
            serviceProvider.GetRequiredService<ILogger<UserManager<ApplicationUser>>>());

        _validator = new CustomPasswordValidator();
        
        // Create test user
        _testUser = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = "testuser@example.com",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }

    public void Dispose()
    {
        _userManager?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateAsync_PasswordContainsUserName_ShouldReturnFailure()
    {
        // Arrange
        var password = "testuser123"; // Contains the username

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordContainsUserName");
        Assert.Equal("Password cannot contain the username.",
            result.Errors.First(e => e.Code == "PasswordContainsUserName").Description);
    }

    #region Null and Empty Password Tests

    [Fact]
    public async Task ValidateAsync_NullPassword_ShouldReturnFailure()
    {
        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, null);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("PasswordRequired", result.Errors.First().Code);
        Assert.Equal("Password is required.", result.Errors.First().Description);
    }

    [Fact]
    public async Task ValidateAsync_EmptyPassword_ShouldReturnFailure()
    {
        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, string.Empty);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("PasswordRequired", result.Errors.First().Code);
        Assert.Equal("Password is required.", result.Errors.First().Description);
    }

    [Fact]
    public async Task ValidateAsync_WhitespacePassword_ShouldReturnFailure()
    {
        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, "   ");

        // Assert
        Assert.False(result.Succeeded);
        Assert.Single(result.Errors);
        Assert.Equal("PasswordHasRepeatingCharacters", result.Errors.First().Code);
    }

    #endregion

    #region Common Password Tests

    [Theory]
    [InlineData("password")]
    [InlineData("123456")]
    [InlineData("qwerty")]
    [InlineData("admin")]
    [InlineData("letmein")]
    [InlineData("welcome")]
    [InlineData("monkey")]
    [InlineData("dragon")]
    public async Task ValidateAsync_CommonPassword_ShouldReturnFailure(string commonPassword)
    {
        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, commonPassword);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "CommonPassword");
        Assert.Equal("Password is too common and easily guessable.", 
            result.Errors.First(e => e.Code == "CommonPassword").Description);
    }

    [Theory]
    [InlineData("PASSWORD")]
    [InlineData("Password")]
    [InlineData("QWERTY")]
    [InlineData("Admin")]
    public async Task ValidateAsync_CommonPasswordDifferentCase_ShouldReturnFailure(string commonPassword)
    {
        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, commonPassword);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "CommonPassword");
    }

    #endregion

    #region Username in Password Tests

    [Fact]
    public async Task ValidateAsync_PasswordContainsUsername_ShouldReturnFailure()
    {
        // Arrange
        var password = "MyTestuserPassword123!";

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordContainsUserName");
        Assert.Equal("Password cannot contain the username.",
            result.Errors.First(e => e.Code == "PasswordContainsUserName").Description);
    }

    [Fact]
    public async Task ValidateAsync_PasswordContainsUsernameUpperCase_ShouldReturnFailure()
    {
        // Arrange
        var password = "MyTESTUSERPassword123!";

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordContainsUserName");
    }

    [Fact]
    public async Task ValidateAsync_PasswordDoesNotContainUsername_ShouldNotFailForUsername()
    {
        // Arrange
        var password = "ComplexPassword123!@#";

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Code == "PasswordContainsUserName");
    }

    [Fact]
    public async Task ValidateAsync_UserWithNullUsername_ShouldNotFailForUsername()
    {
        // Arrange
        var userWithNullUsername = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = null,
            Email = "testuser@example.com"
        };
        var password = "ComplexPassword123!@#";

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithNullUsername, password);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Code == "PasswordContainsUserName");
    }

    #endregion

    #region Email in Password Tests

    [Fact]
    public async Task ValidateAsync_PasswordContainsEmailLocalPart_ShouldReturnFailure()
    {
        // Arrange
        var password = "MyTestuserPassword123!";

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordContainsEmail");
        Assert.Equal("Password cannot contain parts of the email address.",
            result.Errors.First(e => e.Code == "PasswordContainsEmail").Description);
    }

    [Fact]
    public async Task ValidateAsync_PasswordContainsEmailLocalPartUpperCase_ShouldReturnFailure()
    {
        // Arrange
        var password = "MyTESTUSERPassword123!";

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordContainsEmail");
    }

    [Fact]
    public async Task ValidateAsync_PasswordDoesNotContainEmail_ShouldNotFailForEmail()
    {
        // Arrange
        var password = "ComplexPassword123!@#";

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Code == "PasswordContainsEmail");
    }

    [Fact]
    public async Task ValidateAsync_UserWithNullEmail_ShouldNotFailForEmail()
    {
        // Arrange
        var userWithNullEmail = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "testuser",
            Email = null
        };
        var password = "ComplexPassword123!@#";

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithNullEmail, password);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Code == "PasswordContainsEmail");
    }

    #endregion

    #region Repeating Characters Tests

    [Theory]
    [InlineData("Password111")]
    [InlineData("aaa123ABC")]
    [InlineData("Pass@@@word")]
    [InlineData("123AAA456")]
    public async Task ValidateAsync_PasswordWithRepeatingCharacters_ShouldReturnFailure(string password)
    {
        // Arrange
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "differentuser",
            Email = "different@example.com"
        };

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "PasswordHasRepeatingCharacters");
        Assert.Equal("Password cannot have more than 2 consecutive repeating characters.",
            result.Errors.First(e => e.Code == "PasswordHasRepeatingCharacters").Description);
    }

    [Theory]
    [InlineData("Password12")]
    [InlineData("aa123ABC")]
    [InlineData("Pass@@word")]
    [InlineData("123AA456")]
    [InlineData("ComplexP@ssw0rd!")]
    public async Task ValidateAsync_PasswordWithoutExcessiveRepeating_ShouldNotFailForRepeating(string password)
    {
        // Arrange
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "differentuser",
            Email = "different@example.com"
        };

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, password);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Code == "PasswordHasRepeatingCharacters");
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public async Task ValidateAsync_PasswordWithMultipleIssues_ShouldReturnAllErrors()
    {
        // Arrange - password that violates multiple rules
        var password = "testuser111"; // contains username, has repeating chars, and is common pattern

        // Act
        var result = await _validator.ValidateAsync(_userManager, _testUser, password);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.Errors.Count() > 1);
        Assert.Contains(result.Errors, e => e.Code == "PasswordContainsUserName");
        Assert.Contains(result.Errors, e => e.Code == "PasswordHasRepeatingCharacters");
    }

    #endregion

    #region Valid Password Tests

    [Theory]
    [InlineData("ComplexP@ssw0rd!")]
    [InlineData("SecureBank2025#")]
    [InlineData("MyStr0ng!P@ssword")]
    [InlineData("Banking$ystem9")]
    public async Task ValidateAsync_ValidPassword_ShouldReturnSuccess(string validPassword)
    {
        // Arrange
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "differentuser",
            Email = "different@example.com"
        };

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, validPassword);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task ValidateAsync_ShortPasswordThatIsValid_ShouldReturnSuccess()
    {
        // Arrange - short but complex password that doesn't violate custom rules
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "user",
            Email = "user@test.com"
        };
        var password = "Ab1@";

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, password);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_VeryLongValidPassword_ShouldReturnSuccess()
    {
        // Arrange
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "user",
            Email = "user@test.com"
        };
        var password = "ThisIsAVeryLongPasswordThatMeetsAllTheSecurityRequirementsAndDoesNotContainAnyForbiddenPatterns123!@#";

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, password);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_PasswordWithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "user",
            Email = "user@test.com"
        };
        var password = "P@ssw0rd!#$%^&*()_+-=[]{}|;:,.<>?";

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, password);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateAsync_PasswordWithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        var userWithoutConflicts = new ApplicationUser
        {
            Id = "test-user-id",
            UserName = "user",
            Email = "user@test.com"
        };
        var password = "P@ssw0rd123äöü";

        // Act
        var result = await _validator.ValidateAsync(_userManager, userWithoutConflicts, password);

        // Assert
        Assert.True(result.Succeeded);
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public async Task ValidateAsync_PerformanceTest_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var password = "ComplexP@ssw0rd123!";

        // Act
        for (int i = 0; i < 1000; i++)
        {
            await _validator.ValidateAsync(_userManager, _testUser, password);
        }
        stopwatch.Stop();

        // Assert - Should complete 1000 validations within 1 second
        Assert.True(stopwatch.ElapsedMilliseconds < 1000);
    }

    #endregion
}

/// <summary>
/// Mock implementation of IUserStore for testing purposes
/// </summary>
internal class MockUserStore : IUserStore<ApplicationUser>
{
    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult<ApplicationUser?>(null);

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => Task.FromResult<ApplicationUser?>(null);

    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(IdentityResult.Success);

    public void Dispose()
    {
        // No resources to dispose
    }
}
