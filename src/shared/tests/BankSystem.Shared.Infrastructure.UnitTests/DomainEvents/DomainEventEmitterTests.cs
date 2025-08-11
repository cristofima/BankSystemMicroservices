using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Infrastructure.UnitTests.Common;
using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BankSystem.Shared.Infrastructure.UnitTests.DomainEvents;

public class DomainEventEmitterTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DomainEventEmitter _emitter;

    public DomainEventEmitterTests()
    {
        _mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<DomainEventEmitter>>();
        _emitter = new DomainEventEmitter(_mediatorMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task EmitEventsAsync_WithSingleAggregateRoot_ShouldEmitAllEvents()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();

        aggregateRoot.AddEvent(event1);
        aggregateRoot.AddEvent(event2);

        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _emitter.EmitEventsAsync(aggregateRoot, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        aggregateRoot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EmitEventsAsync_WithMultipleAggregateRoots_ShouldEmitAllEvents()
    {
        // Arrange
        var aggregateRoot1 = new TestAggregateRoot();
        var aggregateRoot2 = new TestAggregateRoot();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        var event3 = new TestDomainEvent();

        aggregateRoot1.AddEvent(event1);
        aggregateRoot1.AddEvent(event2);
        aggregateRoot2.AddEvent(event3);

        var aggregateRoots = new[] { aggregateRoot1, aggregateRoot2 };

        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _emitter.EmitEventsAsync(aggregateRoots, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        aggregateRoot1.DomainEvents.Should().BeEmpty();
        aggregateRoot2.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EmitEventsAsync_WithNoEvents_ShouldNotEmitAnything()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();

        // Act
        await _emitter.EmitEventsAsync(aggregateRoot, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task EmitEventsAsync_WithNullAggregateRoot_ShouldNotEmitAnything()
    {
        // Act
        await _emitter.EmitEventsAsync((IAggregateRoot)null!, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task EmitEventsAsync_WithEmptyCollection_ShouldNotEmitAnything()
    {
        // Arrange
        var emptyCollection = Enumerable.Empty<IAggregateRoot>();

        // Act
        await _emitter.EmitEventsAsync(emptyCollection, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task EmitEventsAsync_WhenMediatorThrowsException_ShouldRetryAndEventuallySucceed()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent();
        aggregateRoot.AddEvent(domainEvent);

        var callCount = 0;
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts
                {
                    throw new InvalidOperationException("Test exception");
                }
                return Task.CompletedTask; // Succeed on 3rd attempt
            });

        // Act
        await _emitter.EmitEventsAsync(aggregateRoot, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3)
        );
        aggregateRoot.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task EmitEventsAsync_WhenMediatorFailsAllRetries_ShouldThrowAndKeepEvents()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent();
        aggregateRoot.AddEvent(domainEvent);

        var expectedException = new InvalidOperationException("Test exception");
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _emitter.EmitEventsAsync(aggregateRoot, CancellationToken.None)
        );

        exception.Message.Should().Be("Test exception");

        // Should retry 4 times total (1 initial + 3 retries)
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4)
        );

        // Events should remain because emission was unsuccessful
        aggregateRoot.DomainEvents.Should().Contain(domainEvent);
    }

    [Fact]
    public async Task EmitEventsAsync_WithCancellationToken_ShouldPassTokenToMediator()
    {
        // Arrange
        var aggregateRoot = new TestAggregateRoot();
        var domainEvent = new TestDomainEvent();
        aggregateRoot.AddEvent(domainEvent);

        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _emitter.EmitEventsAsync(aggregateRoot, cancellationTokenSource.Token)
        );

        // The retry policy attempts 4 times total (1 initial + 3 retries) even with cancellation
        _mediatorMock.Verify(
            x => x.Publish(It.IsAny<IDomainEvent>(), cancellationTokenSource.Token),
            Times.Exactly(4)
        );
    }
}
