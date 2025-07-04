using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Security.Application.Configuration;
using Security.Application.Interfaces;
using Security.Domain.Entities;

namespace Security.Application.UnitTests.Common;

/// <summary>
/// Base test class for command handler tests with common mocks and setup
/// </summary>
public abstract class CommandHandlerTestBase : TestBase
{
    protected Mock<UserManager<ApplicationUser>> MockUserManager { get; private set; } = null!;
    protected Mock<ITokenService> MockTokenService { get; private set; } = null!;
    protected Mock<IRefreshTokenService> MockRefreshTokenService { get; private set; } = null!;
    protected Mock<ISecurityAuditService> MockAuditService { get; private set; } = null!;
    protected SecurityOptions SecurityOptions { get; private set; } = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        SetupMocks();
        SetupSecurityOptions();
    }

    private void SetupMocks()
    {
        MockUserManager = CreateMockUserManager();
        MockTokenService = new Mock<ITokenService>();
        MockRefreshTokenService = new Mock<IRefreshTokenService>();
        MockAuditService = new Mock<ISecurityAuditService>();
    }

    private void SetupSecurityOptions()
    {
        SecurityOptions = new SecurityOptions
        {
            MaxFailedLoginAttempts = 5,
            LockoutDuration = TimeSpan.FromMinutes(15),
            PasswordPolicy = new SecurityOptions.PasswordPolicyOptions
            {
                MinLength = 8,
                RequireSpecialCharacters = true,
                RequireNumbers = true,
                RequireUppercase = true,
                RequireLowercase = true
            },
            TokenSecurity = new SecurityOptions.TokenSecurityOptions
            {
                EnableTokenRotation = true,
                EnableRevocationCheck = true,
                MaxConcurrentSessions = 5
            },
            Audit = new SecurityOptions.AuditOptions
            {
                EnableAuditLogging = true,
                LogSuccessfulAuthentication = true,
                LogFailedAuthentication = true,
                LogTokenOperations = true
            }
        };
    }

    /// <summary>
    /// Creates a mock UserManager with proper setup
    /// </summary>
    private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mgr = new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mgr.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mgr.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
        return mgr;
    }

    /// <summary>
    /// Creates a test ApplicationUser with valid properties
    /// </summary>
    protected ApplicationUser CreateTestUser(
        string? id = null,
        string? userName = "testuser@example.com",
        string? email = "testuser@example.com",
        bool isActive = true,
        int failedLoginAttempts = 0)
    {
        return new ApplicationUser
        {
            Id = id ?? Guid.NewGuid().ToString(),
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            IsActive = isActive,
            FailedLoginAttempts = failedLoginAttempts,
            CreatedAt = DateTime.UtcNow,
            ClientId = Guid.NewGuid(),
            RefreshTokens = new List<RefreshToken>()
        };
    }

    /// <summary>
    /// Creates a test RefreshToken with valid properties
    /// </summary>
    protected RefreshToken CreateTestRefreshToken(
        string? userId = null,
        string? token = null,
        string? jwtId = null,
        bool isRevoked = false,
        DateTime? expiryDate = null)
    {
        return new RefreshToken
        {
            Token = token ?? CreateValidRefreshToken(),
            UserId = userId ?? Guid.NewGuid().ToString(),
            JwtId = jwtId ?? Guid.NewGuid().ToString(),
            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            CreatedByIp = CreateValidIpAddress(),
            DeviceInfo = CreateValidDeviceInfo(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an IOptions wrapper for SecurityOptions
    /// </summary>
    protected IOptions<SecurityOptions> CreateSecurityOptions()
    {
        return Options.Create(SecurityOptions);
    }

    /// <summary>
    /// Sets up UserManager to return a specific user for FindByNameAsync
    /// </summary>
    protected void SetupUserManagerFindByName(ApplicationUser? user)
    {
        MockUserManager
            .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
    }

    /// <summary>
    /// Sets up UserManager to return a specific user for FindByEmailAsync
    /// </summary>
    protected void SetupUserManagerFindByEmail(ApplicationUser? user)
    {
        MockUserManager
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
    }

    /// <summary>
    /// Sets up UserManager password check result
    /// </summary>
    protected void SetupUserManagerCheckPassword(bool isValid)
    {
        MockUserManager
            .Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(isValid);
    }

    /// <summary>
    /// Sets up UserManager create user result
    /// </summary>
    protected void SetupUserManagerCreateUser(IdentityResult result)
    {
        MockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    /// <summary>
    /// Sets up UserManager update user result
    /// </summary>
    protected void SetupUserManagerUpdateUser(IdentityResult result)
    {
        MockUserManager
            .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(result);
    }

    /// <summary>
    /// Sets up TokenService to return a specific token
    /// </summary>
    protected void SetupTokenServiceCreateAccessToken(string token, string jwtId, DateTime expiry)
    {
        MockTokenService
            .Setup(x => x.CreateAccessTokenAsync(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<System.Security.Claims.Claim>>()))
            .ReturnsAsync((token, jwtId, expiry));
    }

    /// <summary>
    /// Sets up RefreshTokenService to return a specific refresh token
    /// </summary>
    protected void SetupRefreshTokenServiceCreate(RefreshToken? refreshToken)
    {
        MockRefreshTokenService
            .Setup(x => x.CreateRefreshTokenAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);
    }

    /// <summary>
    /// Sets up UserManager get roles result
    /// </summary>
    protected void SetupUserManagerGetRoles(IList<string> roles)
    {
        MockUserManager
            .Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(roles);
    }
}
