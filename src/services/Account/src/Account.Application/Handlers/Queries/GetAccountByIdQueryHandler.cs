using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Application.Queries;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
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
        ILogger<GetAccountByIdQueryHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository, nameof(accountRepository));
        Guard.AgainstNull(mapper, nameof(mapper));
        Guard.AgainstNull(logger, nameof(logger));

        _accountRepository = accountRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AccountDto>> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation("Retrieving account with ID: {AccountId}", request.AccountId);

            var account = await _accountRepository.GetByIdAsync(
                request.AccountId,
                cancellationToken
            );

            if (account is null)
            {
                _logger.LogWarning("Account {AccountId} not found", request.AccountId);
                return Result<AccountDto>.Failure(
                    $"Account {request.AccountId} not found",
                    ErrorType.NotFound
                );
            }

            var accountDto = _mapper.Map<AccountDto>(account);

            _logger.LogInformation(
                "Successfully retrieved account with ID: {AccountId}",
                request.AccountId
            );
            return Result<AccountDto>.Success(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while retrieving account with ID: {AccountId}",
                request.AccountId
            );
            return Result<AccountDto>.Failure("An error occurred while retrieving the account");
        }
    }
}
