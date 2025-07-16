using MediatR;
using BankSystem.Shared.Domain.Common;
using BankSystem.Account.Application.DTOs;

namespace BankSystem.Account.Application.Queries;

/// <summary>
/// Query to retrieve an account by its account number.
/// </summary>
/// <param name="AccountNumber">The account number to search for</param>
public record GetAccountByNumberQuery(string AccountNumber) : IRequest<Result<AccountDto>>;
