using Security.Application.Configuration;

namespace Security.Application.UnitTests.Configuration;

public class JwtOptionsTests
{
    [Fact]
    public void Default_Constructor_Should_Set_Default_Values()
    {
        // Act
        var options = new JwtOptions();

        // Assert
        Assert.Equal(string.Empty, options.Key);
        Assert.Equal(string.Empty, options.Issuer);
        Assert.Equal(string.Empty, options.Audience);
        Assert.Equal(15, options.AccessTokenExpiryInMinutes);
        Assert.Equal(7, options.RefreshTokenExpiryInDays);
        Assert.True(options.ValidateIssuer);
        Assert.True(options.ValidateAudience);
        Assert.True(options.ValidateLifetime);
        Assert.True(options.ValidateIssuerSigningKey);
    }

    [Fact]
    public void Properties_Can_Be_Set_To_Null_Values()
    {
        // Arrange
        var options = new JwtOptions
        {
            // Act
            Key = null!,
            Issuer = null!,
            Audience = null!
        };

        // Assert
        Assert.Null(options.Key);
        Assert.Null(options.Issuer);
        Assert.Null(options.Audience);
    }

    [Fact]
    public void Properties_Can_Be_Set_To_Empty_Strings()
    {
        // Arrange
        var options = new JwtOptions
        {
            // Act
            Key = string.Empty,
            Issuer = string.Empty,
            Audience = string.Empty
        };

        // Assert
        Assert.Equal(string.Empty, options.Key);
        Assert.Equal(string.Empty, options.Issuer);
        Assert.Equal(string.Empty, options.Audience);
    }

    [Fact]
    public void All_Properties_Can_Be_Set_Together()
    {
        // Arrange
        var options = new JwtOptions
        {
            // Act
            Key = "test-key",
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenExpiryInMinutes = 30,
            RefreshTokenExpiryInDays = 3,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false
        };

        // Assert
        Assert.Equal("test-key", options.Key);
        Assert.Equal("test-issuer", options.Issuer);
        Assert.Equal("test-audience", options.Audience);
        Assert.Equal(30, options.AccessTokenExpiryInMinutes);
        Assert.Equal(3, options.RefreshTokenExpiryInDays);
        Assert.False(options.ValidateIssuer);
        Assert.False(options.ValidateAudience);
        Assert.False(options.ValidateLifetime);
        Assert.False(options.ValidateIssuerSigningKey);
    }
}
