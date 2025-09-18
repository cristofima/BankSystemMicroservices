using BankSystem.Shared.Infrastructure.Configuration;
using BankSystem.Shared.Infrastructure.DomainEvents;
using BankSystem.Shared.Infrastructure.Outbox;
using BankSystem.Shared.Kernel.Common;
using BankSystem.Shared.Kernel.Events;
using BankSystem.Shared.Kernel.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BankSystem.Shared.Infrastructure.IntegrationTests.DomainEvents;

public class DomainEventDispatchInterceptorIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly TestDbContext _dbContext;
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock;
    private readonly Mock<IDomainEventEmitter> _emitterMock;

    public DomainEventDispatchInterceptorIntegrationTests()
    {
        var services = new ServiceCollection();

        // Setup in-memory database
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        // Setup mocks
        _dispatcherMock = new Mock<IDomainEventDispatcher>();
        _emitterMock = new Mock<IDomainEventEmitter>();

        services.AddSingleton(_dispatcherMock.Object);
        services.AddSingleton(_emitterMock.Object);
        services.AddLogging();

        // Register the interceptor
        services.AddScoped<DomainEventDispatchInterceptor>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<TestDbContext>();
    }

    [Fact]
    public async Task SaveChangesAsync_WithDomainEventsInContext_ShouldDispatchEvents()
    {
        // Arrange
        var interceptor = _serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();
        var testEntity = new TestEntityWithEvents();
        var domainEvent = new TestDomainEvent();

        testEntity.AddEvent(domainEvent);

        _dbContext.TestEntities.Add(testEntity);
        _dbContext.ChangeTracker.Entries<IAggregateRoot>().Should().HaveCount(1);

        // Act
        await interceptor.SavingChangesAsync(
            _dbContext,
            new InterceptionResult<int>(),
            CancellationToken.None
        );

        // Assert
        _dispatcherMock.Verify(
            x =>
                x.DispatchAsync(
                    It.Is<IEnumerable<IDomainEvent>>(events => events.Contains(domainEvent)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        // Verify events were cleared from entity
        testEntity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleAggregateRoots_ShouldDispatchAllEvents()
    {
        // Arrange
        var interceptor = _serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();

        var entity1 = new TestEntityWithEvents();
        var entity2 = new TestEntityWithEvents();
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        var event3 = new TestDomainEvent();

        entity1.AddEvent(event1);
        entity1.AddEvent(event2);
        entity2.AddEvent(event3);

        _dbContext.TestEntities.AddRange(entity1, entity2);

        var capturedEvents = new List<IDomainEvent>();
        _dispatcherMock
            .Setup(x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<IEnumerable<IDomainEvent>, CancellationToken>(
                (events, _) => capturedEvents.AddRange(events)
            );

        // Act
        await interceptor.SavingChangesAsync(
            _dbContext,
            new InterceptionResult<int>(),
            CancellationToken.None
        );

        // Assert
        capturedEvents.Should().HaveCount(3);
        capturedEvents.Should().Contain(event1);
        capturedEvents.Should().Contain(event2);
        capturedEvents.Should().Contain(event3);

        entity1.DomainEvents.Should().BeEmpty();
        entity2.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoDomainEvents_ShouldNotDispatch()
    {
        // Arrange
        var interceptor = _serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();
        var testEntity = new TestEntityWithEvents(); // No events added

        _dbContext.TestEntities.Add(testEntity);

        // Act
        await interceptor.SavingChangesAsync(
            _dbContext,
            new InterceptionResult<int>(),
            CancellationToken.None
        );

        // Assert
        _dispatcherMock.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoAggregateRoots_ShouldNotDispatch()
    {
        // Arrange
        var interceptor = _serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();
        var normalEntity = new TestEntity { Name = "Test" };

        _dbContext.NormalEntities.Add(normalEntity);

        // Act
        await interceptor.SavingChangesAsync(
            _dbContext,
            new InterceptionResult<int>(),
            CancellationToken.None
        );

        // Assert
        _dispatcherMock.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDispatcherThrows_ShouldPropagateException()
    {
        // Arrange
        var interceptor = _serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();
        var testEntity = new TestEntityWithEvents();
        var domainEvent = new TestDomainEvent();
        var expectedException = new InvalidOperationException("Dispatcher error");

        testEntity.AddEvent(domainEvent);
        _dbContext.TestEntities.Add(testEntity);

        _dispatcherMock
            .Setup(x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                interceptor.SavingChangesAsync(
                    _dbContext,
                    new InterceptionResult<int>(),
                    CancellationToken.None
                )
        );

        exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var interceptor = _serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();
        var testEntity = new TestEntityWithEvents();
        var domainEvent = new TestDomainEvent();
        var cancellationToken = new CancellationToken();

        testEntity.AddEvent(domainEvent);
        _dbContext.TestEntities.Add(testEntity);

        // Act
        await interceptor.SavingChangesAsync(
            _dbContext,
            new InterceptionResult<int>(),
            cancellationToken
        );

        // Assert
        _dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), cancellationToken),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveChangesAsync_WithOutboxOptions_ShouldUseEmitterWhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        var outboxOptions = new OutboxOptions { Enabled = true };
        services.AddSingleton(_emitterMock.Object);
        services.AddSingleton(outboxOptions);
        services.AddLogging();
        services.AddScoped<DomainEventDispatchInterceptor>();

        using var serviceProvider = services.BuildServiceProvider();
        using var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
        var interceptor = serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();

        var testEntity = new TestEntityWithEvents();
        var domainEvent = new TestDomainEvent();
        testEntity.AddEvent(domainEvent);
        dbContext.TestEntities.Add(testEntity);

        // Act
        await interceptor.SavingChangesAsync(
            dbContext,
            new InterceptionResult<int>(),
            CancellationToken.None
        );

        // Assert
        _emitterMock.Verify(
            x =>
                x.EmitAsync(It.IsAny<IEnumerable<IAggregateRoot>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task SaveChangesAsync_WithOutboxDisabled_ShouldUseDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())
        );

        var outboxOptions = new OutboxOptions { Enabled = false };
        services.AddSingleton(_dispatcherMock.Object);
        services.AddSingleton(outboxOptions);
        services.AddLogging();
        services.AddScoped<DomainEventDispatchInterceptor>();

        using var serviceProvider = services.BuildServiceProvider();
        using var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
        var interceptor = serviceProvider.GetRequiredService<DomainEventDispatchInterceptor>();

        var testEntity = new TestEntityWithEvents();
        var domainEvent = new TestDomainEvent();
        testEntity.AddEvent(domainEvent);
        dbContext.TestEntities.Add(testEntity);

        // Act
        await interceptor.SavingChangesAsync(
            dbContext,
            new InterceptionResult<int>(),
            CancellationToken.None
        );

        // Assert
        _dispatcherMock.Verify(
            x =>
                x.DispatchAsync(
                    It.IsAny<IEnumerable<IDomainEvent>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}

// Test helper classes
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    public DbSet<TestEntityWithEvents> TestEntities { get; set; } = null!;
    public DbSet<TestEntity> NormalEntities { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntityWithEvents>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        base.OnModelCreating(modelBuilder);
    }
}

public class TestEntityWithEvents : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public record TestDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    public int Version { get; init; } = 1;
}
