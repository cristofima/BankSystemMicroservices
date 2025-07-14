using BankSystem.Account.Application.DTOs;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Queries;

public record GetAccountByIdQuery(string AccountNumber) : IRequest<Result<AccountDto>>;
