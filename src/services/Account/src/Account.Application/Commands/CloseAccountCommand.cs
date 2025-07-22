using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Commands;

public sealed record CloseAccountCommand(
    Guid AccountId,
    string Reason) : IRequest<Result>, IValidationRequest
{
    public string ValidationErrorTitle() => "Account Closure Failed";
}