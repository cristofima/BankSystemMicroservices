using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Commands;

public sealed record ActivateAccountCommand(
    Guid AccountId) : IRequest<Result>, IValidationRequest
{
    public string ValidationErrorTitle() => "Account Activation Failed";
}