using BankSystem.Shared.Kernel.Common;

namespace BankSystem.Account.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Test implementation of ICurrentUser for integration tests
/// </summary>
public class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public Guid CustomerId { get; init; } = Guid.NewGuid();
    public string UserName { get; init; } = "test-user";
}
