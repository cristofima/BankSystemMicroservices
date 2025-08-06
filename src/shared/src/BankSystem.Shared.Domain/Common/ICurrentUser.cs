namespace BankSystem.Shared.Domain.Common;

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid CustomerId { get; }
    string UserName { get; }
}
