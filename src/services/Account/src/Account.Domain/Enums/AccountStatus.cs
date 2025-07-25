namespace BankSystem.Account.Domain.Enums;

/// <summary>
/// Lifecycle state of an account.
/// </summary>
public enum AccountStatus
{
    PendingActivation = 0,
    Active = 1,
    Suspended = 2,
    Frozen = 3,
    Closed = 4
}