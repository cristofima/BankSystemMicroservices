namespace BankSystem.Account.Application.Commands;

public sealed record ActivateAccountCommand(Guid AccountId) : IAccountActionCommand
{
    public string ValidationErrorTitle() => "Account Activation Failed";
}
