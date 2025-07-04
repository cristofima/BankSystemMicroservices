using AutoFixture;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Security.Application.UnitTests.Common;

/// <summary>
/// Base test class providing common testing utilities and setup
/// </summary>
public abstract class TestBase
{
    protected IFixture Fixture { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        
        // Configure fixture to create valid GUIDs
        Fixture.Register(() => Guid.NewGuid());
    }

    /// <summary>
    /// Creates a mock logger for the specified type
    /// </summary>
    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Creates an entity using AutoFixture
    /// </summary>
    protected T CreateEntity<T>() where T : class
    {
        return Fixture.Create<T>();
    }

    /// <summary>
    /// Creates multiple entities using AutoFixture
    /// </summary>
    protected List<T> CreateMany<T>(int count = 3) where T : class
    {
        return Fixture.CreateMany<T>(count).ToList();
    }

    /// <summary>
    /// Creates a CancellationToken that is not cancelled
    /// </summary>
    protected CancellationToken CreateCancellationToken()
    {
        return CancellationToken.None;
    }

    /// <summary>
    /// Creates a valid IP address string for testing
    /// </summary>
    protected string CreateValidIpAddress()
    {
        return "192.168.1.1";
    }

    /// <summary>
    /// Creates a valid device info string for testing
    /// </summary>
    protected string CreateValidDeviceInfo()
    {
        return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
    }

    /// <summary>
    /// Creates a valid JWT token for testing purposes
    /// </summary>
    protected string CreateValidJwtToken()
    {
        return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
    }

    /// <summary>
    /// Creates a valid refresh token for testing purposes
    /// </summary>
    protected string CreateValidRefreshToken()
    {
        return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
