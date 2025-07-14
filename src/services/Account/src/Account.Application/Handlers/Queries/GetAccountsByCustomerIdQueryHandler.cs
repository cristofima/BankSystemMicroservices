using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Queries;
using AutoMapper;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Queries;

public class GetAccountsByCustomerIdQueryHandler : IRequestHandler<GetAccountsByCustomerIdQuery, Result<IEnumerable<AccountDto>>>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAccountsByCustomerIdQueryHandler> _logger;

    public GetAccountsByCustomerIdQueryHandler(
        IAccountRepository accountRepository,
        IMapper mapper,
        ILogger<GetAccountsByCustomerIdQueryHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<IEnumerable<AccountDto>>> Handle(
        GetAccountsByCustomerIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving accounts for customer {CustomerId}", request.CustomerId);

            var accounts = await _accountRepository.GetByCustomerIdAsync(request.CustomerId, cancellationToken);

            var accountDtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);

            _logger.LogInformation("Found {Count} accounts for customer {CustomerId}", 
                accountDtos.Count(), request.CustomerId);

            return Result<IEnumerable<AccountDto>>.Success(accountDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts for customer {CustomerId}", request.CustomerId);
            return Result<IEnumerable<AccountDto>>.Failure("An error occurred while retrieving customer accounts");
        }
    }
}
