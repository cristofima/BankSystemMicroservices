using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Commands;

public class ActivateAccountCommandHandler : IRequestHandler<ActivateAccountCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<ActivateAccountCommandHandler> _logger;

    public ActivateAccountCommandHandler(
        IAccountRepository accountRepository,
        ILogger<ActivateAccountCommandHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository, nameof(accountRepository));
        Guard.AgainstNull(logger, nameof(logger));

        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        ActivateAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation("Activating account {AccountId}", request.AccountId);
            var account = await _accountRepository.GetByIdAsync(
                request.AccountId,
                cancellationToken
            );
            if (account is null)
            {
                _logger.LogWarning(
                    "Account {AccountId} not found for activation",
                    request.AccountId
                );
                return Result.Failure($"Account {request.AccountId} not found", ErrorType.NotFound);
            }

            // Activate the account
            var activateResult = account.Activate();
            if (!activateResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to activate account {AccountId}: {Error}",
                    request.AccountId,
                    activateResult.Error
                );
                return activateResult;
            }

            await _accountRepository.UpdateAsync(account, cancellationToken);
            _logger.LogInformation("Account {AccountId} activated successfully", request.AccountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating account {AccountId}", request.AccountId);
            return Result.Failure($"Error activating account: {ex.Message}");
        }
    }
}
