using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Commands;

public sealed record SuspendAccountCommand(
    Guid AccountId,
    string Reason) : IRequest<Result>, IValidationRequest
{
    public string ValidationErrorTitle() => "Account Suspension Failed";
}