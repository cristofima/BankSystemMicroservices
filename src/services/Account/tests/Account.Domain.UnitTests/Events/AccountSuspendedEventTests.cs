using Account.Domain.Events;
using FluentAssertions;

namespace Account.Domain.UnitTests.Events;

public class AccountSuspendedEventTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var reason = "Suspicious activity";
        var suspendedAt = DateTime.UtcNow;
        var suspendedBy = "System";

        // Act
        var @event = new AccountSuspendedEvent(accountId, reason, suspendedAt, suspendedBy);

        // Assert
        @event.AccountId.Should().Be(accountId);
        @event.Reason.Should().Be(reason);
        @event.SuspendedAt.Should().Be(suspendedAt);
        @event.SuspendedBy.Should().Be(suspendedBy);
        @event.Id.Should().NotBeEmpty();
        @event.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
