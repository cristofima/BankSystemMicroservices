using Bogus;
using Microsoft.Extensions.Logging;
using Security.Infrastructure.Data;
using Testcontainers.MsSql;

namespace Security.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Base class for Security infrastructure integration tests with database and container setup
/// </summary>
public abstract class SecurityTestBase
{
    protected SecurityDbContext DbContext { get; private set; } = null!;
    protected IServiceProvider ServiceProvider { get; private set; } = null!;
    protected Faker Faker { get; private set; } = null!;

    private MsSqlContainer _mssqlContainer = null!;
    private ServiceProvider _serviceProvider = null!;

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        // Initialize faker for test data generation
        Faker = new Faker();

        // Start SQL Server container
        _mssqlContainer = new MsSqlBuilder()
            .WithPassword("TestPassword123!")
            .WithCleanUp(true)
            .Build();

        await _mssqlContainer.StartAsync();

        // Setup services
        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();
        ServiceProvider = _serviceProvider;

        // Get DbContext and ensure database is created
        DbContext = ServiceProvider.GetRequiredService<SecurityDbContext>();
        await DbContext.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task DisposeAsync()
    {
        try
        {
            // Close all connections first
            if (DbContext?.Database?.GetDbConnection()?.State == System.Data.ConnectionState.Open)
            {
                await DbContext.Database.GetDbConnection().CloseAsync();
            }
            
            // Dispose DbContext first
            if (DbContext != null)
            {
                await DbContext.DisposeAsync();
            }
            
            // Dispose service provider
            if (_serviceProvider != null)
            {
                await _serviceProvider.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the teardown
            Console.WriteLine($"Warning during teardown: {ex.Message}");
        }
        finally
        {
            // Always dispose the container
            try
            {
                await _mssqlContainer.StopAsync();
                await _mssqlContainer.DisposeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning during container disposal: {ex.Message}");
            }
        }
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Add DbContext with SQL Server
        services.AddDbContext<SecurityDbContext>(options =>
        {
            options.UseSqlServer(_mssqlContainer.GetConnectionString());
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Add basic logging
        services.AddLogging(builder => builder.AddConsole());

        // Add basic options for services that need them
        services.Configure<Security.Application.Configuration.JwtOptions>(options =>
        {
            options.Key = "test-jwt-key-for-integration-tests-32-characters-long";
            options.Issuer = "test-issuer";
            options.Audience = "test-audience";
            options.AccessTokenExpiryInMinutes = 15;
            options.RefreshTokenExpiryInDays = 7;
        });

        services.Configure<Security.Application.Configuration.SecurityOptions>(options =>
        {
            options.MaxFailedLoginAttempts = 5;
            options.LockoutDuration = TimeSpan.FromMinutes(15);
        });

        // Try to add services if interfaces exist
        try
        {
            // Register services from Infrastructure layer
            services.AddScoped<Security.Application.Interfaces.IRefreshTokenService, Security.Infrastructure.Services.RefreshTokenService>();
        }
        catch
        {
            // Services might not be fully implemented yet
        }

        try
        {
            services.AddScoped<Security.Application.Interfaces.ISecurityAuditService, Security.Infrastructure.Services.SecurityAuditService>();
        }
        catch
        {
            // Services might not be fully implemented yet
        }

        try
        {
            services.AddScoped<Security.Application.Interfaces.ITokenService, Security.Infrastructure.Services.TokenService>();
        }
        catch
        {
            // Services might not be fully implemented yet
        }
    }

    // Helper methods for test data creation
    protected async Task<ApplicationUser> CreateTestUserAsync(string? email = null, string? firstName = null, string? lastName = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email ?? Faker.Internet.Email(),
            Email = email ?? Faker.Internet.Email(),
            FirstName = firstName ?? Faker.Name.FirstName(),
            LastName = lastName ?? Faker.Name.LastName(),
            ClientId = Guid.NewGuid(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();
        return user;
    }

    protected async Task<List<ApplicationUser>> CreateTestUsersAsync(int count)
    {
        var users = new List<ApplicationUser>();
        for (int i = 0; i < count; i++)
        {
            var user = await CreateTestUserAsync();
            users.Add(user);
        }
        return users;
    }

    protected async Task<RefreshToken> CreateTestRefreshTokenAsync(string userId, bool isRevoked = false, DateTime? expiryDate = null)
    {
        var token = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),
            JwtId = Guid.NewGuid().ToString(),
            UserId = userId,
            ExpiryDate = expiryDate ?? DateTime.UtcNow.AddDays(7),
            IsRevoked = isRevoked,
            CreatedByIp = Faker.Internet.Ip(),
            CreatedAt = DateTime.UtcNow
        };

        if (isRevoked)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevocationReason = "Test revocation";
            token.RevokedByIp = Faker.Internet.Ip();
        }

        DbContext.RefreshTokens.Add(token);
        await DbContext.SaveChangesAsync();
        return token;
    }

    protected string GenerateTestToken()
    {
        return Convert.ToBase64String(Faker.Random.Bytes(32));
    }
}
