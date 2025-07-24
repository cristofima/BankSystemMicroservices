namespace BankSystem.Account.Application.Commands;

public sealed record SuspendAccountCommand(
    Guid AccountId,
    string Reason) : IAccountActionCommand
{
    public string ValidationErrorTitle() => "Account Suspension Failed";
}