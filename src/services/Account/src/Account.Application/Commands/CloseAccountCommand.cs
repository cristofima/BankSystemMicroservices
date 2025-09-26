namespace BankSystem.Account.Application.Commands;

public sealed record CloseAccountCommand(Guid AccountId, string Reason) : IAccountActionCommand
{
    public string ValidationErrorTitle() => "Account Closure Failed";
}
