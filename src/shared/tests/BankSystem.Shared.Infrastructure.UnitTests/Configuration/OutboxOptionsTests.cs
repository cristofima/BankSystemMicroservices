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
            [$"{MassTransitOptions.SectionName}:Outbox:QueryDelaySeconds"] = "2",
            [$"{MassTransitOptions.SectionName}:Outbox:DuplicateDetectionWindowMinutes"] = "10",
            [$"{MassTransitOptions.SectionName}:Outbox:DisableInboxCleanupService"] = "true",
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.Configure<MassTransitOptions>(
            configuration.GetSection(MassTransitOptions.SectionName)
        );

        // Act
        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<MassTransitOptions>>().Value;

        // Assert
        options.Outbox.QueryDelaySeconds.Should().Be(2);
        options.Outbox.DuplicateDetectionWindowMinutes.Should().Be(10);
        options.Outbox.DisableInboxCleanupService.Should().BeTrue();
    }

    [Fact]
    public void OutboxOptions_WithDefaults_ShouldHaveExpectedValues()
    {
        // Arrange & Act
        var options = new MassTransitOptions();

        // Assert
        options.Outbox.QueryDelaySeconds.Should().Be(1);
        options.Outbox.DuplicateDetectionWindowMinutes.Should().Be(5);
        options.Outbox.DisableInboxCleanupService.Should().BeFalse();
    }

    [Fact]
    public void OutboxOptions_SectionName_ShouldBeCorrect()
    {
        // Act & Assert
        MassTransitOptions.SectionName.Should().Be("MassTransit");
    }
}
