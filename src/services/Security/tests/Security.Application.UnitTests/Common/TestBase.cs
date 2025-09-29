using System.Security.Cryptography;
using AutoFixture;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Security.Application.Mapping;

namespace Security.Application.UnitTests.Common;

/// <summary>
/// Base test class providing common testing utilities and setup
/// </summary>
public abstract class TestBase
{
    private Fixture Fixture { get; }
    protected IMapper Mapper { get; }

    protected TestBase()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        // Configure fixture to create valid GUIDs
        Fixture.Register(Guid.NewGuid);

        // Configure AutoMapper
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserContactMappingProfile>();
        });
        Mapper = mapperConfig.CreateMapper();
    }

    /// <summary>
    /// Creates a mock logger for the specified type
    /// </summary>
    protected static Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Creates a CancellationToken that is not cancelled
    /// </summary>
    protected static CancellationToken CreateCancellationToken()
    {
        return CancellationToken.None;
    }

    /// <summary>
    /// Creates a valid IP address string for testing
    /// </summary>
    protected static string CreateValidIpAddress()
    {
        return "192.168.1.1";
    }

    /// <summary>
    /// Creates a valid device info string for testing
    /// </summary>
    protected static string CreateValidDeviceInfo()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
    }

    /// <summary>
    /// Creates a valid JWT token for testing purposes
    /// </summary>
    protected static string CreateValidJwtToken()
    {
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
    }

    /// <summary>
    /// Creates a valid refresh token for testing purposes
    /// </summary>
    protected static string CreateValidRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
