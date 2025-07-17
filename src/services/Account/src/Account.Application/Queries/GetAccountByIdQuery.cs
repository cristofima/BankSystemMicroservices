using BankSystem.Account.Application.DTOs;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Queries;

public record GetAccountByIdQuery(Guid AccountId) : IRequest<Result<AccountDto>>;
