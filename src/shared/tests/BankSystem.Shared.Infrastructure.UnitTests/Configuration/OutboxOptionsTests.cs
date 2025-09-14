using BankSystem.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BankSystem.Shared.Infrastructure.UnitTests.Configuration;

public class OutboxOptionsTests
{
    [Fact]
    public void OutboxOptions_WithValidConfiguration_ShouldBindCorrectly()
    {
        // Arrange
        var configurationData = new Dictionary<string, string>
        {
            [$"{OutboxOptions.SectionName}:QueryDelaySeconds"] = "2",
            [$"{OutboxOptions.SectionName}:DuplicateDetectionWindowMinutes"] = "10",
            [$"{OutboxOptions.SectionName}:DisableInboxCleanupService"] = "true",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<OutboxOptions>>().Value;

        // Assert
        options.QueryDelaySeconds.Should().Be(2);
        options.DuplicateDetectionWindowMinutes.Should().Be(10);
        options.DisableInboxCleanupService.Should().BeTrue();
    }

    [Fact]
    public void OutboxOptions_WithDefaults_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var options = new OutboxOptions();

        // Assert
        options.QueryDelaySeconds.Should().Be(1);
        options.DuplicateDetectionWindowMinutes.Should().Be(5);
        options.DisableInboxCleanupService.Should().BeFalse();
    }

    [Fact]
    public void OutboxOptions_SectionName_ShouldBeCorrect()
    {
        // Act & Assert
        OutboxOptions.SectionName.Should().Be("MassTransit.Outbox");
    }
}
