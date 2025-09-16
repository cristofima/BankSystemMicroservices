using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Infrastructure.UnitTests.Common;
using BankSystem.Shared.Kernel.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BankSystem.Shared.Infrastructure.UnitTests.DomainEvents;

public class DomainEventDispatcherTests
{
    private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
    private readonly DomainEventDispatcher _dispatcher;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    public DomainEventDispatcherTests()
    {
        var mockMediator = new Mock<IMediator>();
        _mockPublishEndpoint = new Mock<IPublishEndpoint>();
        var mockLogger = new Mock<ILogger<DomainEventDispatcher>>();
        _dispatcher = new DomainEventDispatcher(
            mockMediator.Object,
            _mockPublishEndpoint.Object,
            mockLogger.Object
        );
    }

    [Fact]
    public async Task DispatchEventsAsync_WithValidEntity_ShouldPublishEvents()
    {
        // Arrange
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();
        entity.AddDomainEvent(domainEvent);

        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchEventsAsync(entity, _cancellationToken);

        // Assert
        _mockPublishEndpoint.Verify(
            x =>
                x.Publish(
                    It.Is<IDomainEvent>(e => e.EventId == domainEvent.EventId),
                    _cancellationToken
                ),
            Times.Once
        );

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithEntityWithoutEvents_ShouldNotPublish()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        await _dispatcher.DispatchEventsAsync(entity, _cancellationToken);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DispatchEventsAsync_WithMultipleEvents_ShouldPublishAllEvents()
    {
        // Arrange
        var entity = new TestEntity();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        entity.AddDomainEvent(event1);
        entity.AddDomainEvent(event2);

        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), _cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _dispatcher.DispatchEventsAsync(entity, _cancellationToken);

        // Assert
        _mockPublishEndpoint.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), _cancellationToken),
            Times.Exactly(2)
        );

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WhenPublishFails_ShouldNotClearEvents()
    {
        // Arrange
        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();
        entity.AddDomainEvent(domainEvent);

        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), _cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Publish failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _dispatcher.DispatchEventsAsync(entity, _cancellationToken)
        );

        // Events should not be cleared when publish fails
        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task DispatchEventsAsync_WithCancellation_ShouldRespectCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var entity = new TestEntity();
        var domainEvent = new TestDomainEvent();
        entity.AddDomainEvent(domainEvent);

        _mockPublishEndpoint
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _dispatcher.DispatchEventsAsync(entity, cts.Token)
        );
    }
}
