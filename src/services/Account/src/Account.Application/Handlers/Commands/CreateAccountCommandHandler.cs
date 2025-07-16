using AccountEntity = BankSystem.Account.Domain.Entities.Account;
using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.DTOs;
using AutoMapper;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Commands;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<AccountDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        ILogger<CreateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating account for customer {CustomerId}", request.CustomerId);

            var currency = new Currency(request.Currency);

            // Create new account
            var account = AccountEntity.CreateNew(
                request.CustomerId,
                request.AccountType,
                currency);

            // Save account
            await _accountRepository.AddAsync(account, cancellationToken);

            var accountDto = _mapper.Map<AccountDto>(account);

            _logger.LogInformation("Account {AccountId} created successfully for customer {CustomerId}",
                account.Id, request.CustomerId);

            return Result<AccountDto>.Success(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for customer {CustomerId}", request.CustomerId);
            return Result<AccountDto>.Failure("An error occurred while creating the account");
        }
    }
}