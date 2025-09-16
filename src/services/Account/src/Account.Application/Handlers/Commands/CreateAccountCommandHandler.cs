using AutoMapper;
using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using BankSystem.Shared.Domain.ValueObjects;
using BankSystem.Shared.Kernel.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.Handlers.Commands;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Result<AccountDto>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateAccountCommandHandler> _logger;

    public CreateAccountCommandHandler(
        IAccountRepository accountRepository,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<CreateAccountCommandHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository);
        Guard.AgainstNull(currentUser);
        Guard.AgainstNull(mapper);
        Guard.AgainstNull(logger);

        _accountRepository = accountRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AccountDto>> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        var customerId = _currentUser.CustomerId;

        try
        {
            _logger.LogInformation("Creating account for customer {CustomerId}", customerId);

            var currency = new Currency(request.Currency);

            // Create new account
            var account = AccountEntity.CreateNew(customerId, request.AccountType, currency);

            // Save account
            await _accountRepository.AddAsync(account, cancellationToken);

            var accountDto = _mapper.Map<AccountDto>(account);

            _logger.LogInformation(
                "Account {AccountId} created successfully for customer {CustomerId}",
                account.Id,
                customerId
            );

            return Result<AccountDto>.Success(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account for customer {CustomerId}", customerId);
            return Result<AccountDto>.Failure("An error occurred while creating the account");
        }
    }
}
