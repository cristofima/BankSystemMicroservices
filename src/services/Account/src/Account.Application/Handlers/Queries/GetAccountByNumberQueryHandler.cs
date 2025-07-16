using MediatR;
using AutoMapper;
using Microsoft.Extensions.Logging;
using BankSystem.Shared.Domain.Common;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Queries;
using BankSystem.Account.Application.Interfaces;

namespace BankSystem.Account.Application.Handlers.Queries;

/// <summary>
/// Handler for retrieving an account by its account number.
/// </summary>
public class GetAccountByNumberQueryHandler : IRequestHandler<GetAccountByNumberQuery, Result<AccountDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAccountByNumberQueryHandler> _logger;

    public GetAccountByNumberQueryHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        ILogger<GetAccountByNumberQueryHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AccountDto>> Handle(GetAccountByNumberQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting account by number {AccountNumber}", request.AccountNumber);

            if (string.IsNullOrWhiteSpace(request.AccountNumber))
            {
                _logger.LogWarning("Account number is null or empty");
                return Result<AccountDto>.Failure("Account number cannot be null or empty");
            }

            var account = await _accountRepository.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);

            if (account == null)
            {
                _logger.LogInformation("Account with number {AccountNumber} not found", request.AccountNumber);
                return Result<AccountDto>.Failure("Account not found");
            }

            var accountDto = _mapper.Map<AccountDto>(account);

            _logger.LogInformation("Successfully retrieved account {AccountId} by number {AccountNumber}", 
                account.Id, request.AccountNumber);

            return Result<AccountDto>.Success(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting account by number {AccountNumber}", request.AccountNumber);
            return Result<AccountDto>.Failure("An error occurred while retrieving the account");
        }
    }
}
