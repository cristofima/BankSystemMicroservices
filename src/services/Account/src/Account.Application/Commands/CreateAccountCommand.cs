using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Commands;

public sealed record CreateAccountCommand(
    AccountType AccountType,
    string Currency = "USD") : IRequest<Result<AccountDto>>, IValidationRequest
{
    public string ValidationErrorTitle() => "Account Creation Failed";
}