using BankSystem.Account.Application.DTOs;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Queries;

public record GetAccountsByCustomerIdQuery : IRequest<Result<IEnumerable<AccountDto>>>;