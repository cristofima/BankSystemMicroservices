using MediatR;
using BankSystem.Shared.Domain.Common;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Domain.Enums;

namespace BankSystem.Account.Application.Commands;

public record CreateAccountCommand(
    Guid CustomerId,
    AccountType AccountType,
    string Currency = "USD") : IRequest<Result<AccountDto>>;