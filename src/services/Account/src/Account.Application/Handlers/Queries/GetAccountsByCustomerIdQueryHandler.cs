using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Application.Queries;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Queries;

public class GetAccountsByCustomerIdQueryHandler
    : IRequestHandler<GetAccountsByCustomerIdQuery, Result<IEnumerable<AccountDto>>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAccountsByCustomerIdQueryHandler> _logger;

    public GetAccountsByCustomerIdQueryHandler(
        IAccountRepository accountRepository,
        ICurrentUser currentUser,
        IMapper mapper,
        ILogger<GetAccountsByCustomerIdQueryHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository, "accountRepository");
        Guard.AgainstNull(currentUser, "currentUser");
        Guard.AgainstNull(mapper, "mapper");
        Guard.AgainstNull(logger, "logger");

        _accountRepository = accountRepository;
        _currentUser = currentUser;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<AccountDto>>> Handle(
        GetAccountsByCustomerIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var customerId = _currentUser.CustomerId;

        try
        {
            _logger.LogInformation("Retrieving accounts for customer {CustomerId}", customerId);

            var accounts = await _accountRepository.GetByCustomerIdAsync(
                customerId,
                cancellationToken
            );
            _logger.LogInformation(
                "Found {Count} accounts for customer {CustomerId}",
                accounts.Count(),
                customerId
            );

            if (!accounts.Any())
            {
                return Result<IEnumerable<AccountDto>>.Failure(
                    "No accounts found",
                    ErrorType.NotFound
                );
            }

            var accountDtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);
            return Result<IEnumerable<AccountDto>>.Success(accountDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts for customer {CustomerId}", customerId);
            return Result<IEnumerable<AccountDto>>.Failure(
                "An error occurred while retrieving customer accounts"
            );
        }
    }
}
