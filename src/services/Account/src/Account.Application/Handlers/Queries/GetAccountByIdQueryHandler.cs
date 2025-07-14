using MediatR;
using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Queries;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Queries;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, Result<AccountDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAccountByIdQueryHandler> _logger;

    public GetAccountByIdQueryHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        ILogger<GetAccountByIdQueryHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving account with number: {AccountNumber}", request.AccountNumber);

            var account = await _accountRepository.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);

            if (account == null)
            {
                _logger.LogWarning("Account not found with number: {AccountNumber}", request.AccountNumber);
                return Result<AccountDto>.Failure("Account not found");
            }

            var accountDto = _mapper.Map<AccountDto>(account);

            _logger.LogInformation("Successfully retrieved account with number: {AccountNumber}", request.AccountNumber);
            return Result<AccountDto>.Success(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving account with number: {AccountNumber}", request.AccountNumber);
            return Result<AccountDto>.Failure("An error occurred while retrieving the account");
        }
    }
}
