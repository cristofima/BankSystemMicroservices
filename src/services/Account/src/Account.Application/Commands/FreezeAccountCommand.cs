namespace BankSystem.Account.Application.Commands;

public sealed record FreezeAccountCommand(
    Guid AccountId,
    string Reason) : IAccountActionCommand
{
    public string ValidationErrorTitle() => "Account Freezing Failed";
}