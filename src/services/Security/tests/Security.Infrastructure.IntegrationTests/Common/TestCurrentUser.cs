using BankSystem.Shared.Kernel.Common;

namespace Security.Infrastructure.IntegrationTests.Common;

/// <summary>
/// Test implementation of ICurrentUser for integration tests
/// </summary>
public class TestCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = "test-user";
}
