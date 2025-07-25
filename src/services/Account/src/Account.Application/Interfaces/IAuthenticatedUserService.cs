namespace BankSystem.Account.Application.Interfaces;

public interface IAuthenticatedUserService
{
    Guid UserId { get; }
    Guid CustomerId { get; }
    string UserName { get; }
}