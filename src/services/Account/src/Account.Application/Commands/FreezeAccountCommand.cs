using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Commands;

public sealed record FreezeAccountCommand(
    Guid AccountId,
    string Reason) : IRequest<Result>, IValidationRequest
{
    public string ValidationErrorTitle() => "Account Freezing Failed";
}