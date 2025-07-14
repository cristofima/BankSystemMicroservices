using BankSystem.Account.Application.DTOs;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Queries;

public record GetAccountsByCustomerIdQuery(Guid CustomerId) : IRequest<Result<IEnumerable<AccountDto>>>;
