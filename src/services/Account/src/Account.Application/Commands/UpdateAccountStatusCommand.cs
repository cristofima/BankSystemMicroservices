using MediatR;
using BankSystem.Shared.Domain.Common;
using BankSystem.Account.Domain.Enums;

namespace BankSystem.Account.Application.Commands;

/// <summary>
/// Command to update an account's status.
/// </summary>
/// <param name="AccountId">The account ID to update</param>
/// <param name="Status">The new status for the account</param>
/// <param name="Reason">The reason for the status change</param>
public record UpdateAccountStatusCommand(
    Guid AccountId,
    AccountStatus Status,
    string? Reason = null) : IRequest<Result>;
