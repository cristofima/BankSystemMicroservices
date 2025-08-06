using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Commands;

public class CloseAccountCommandHandler : IRequestHandler<CloseAccountCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CloseAccountCommandHandler> _logger;

    public CloseAccountCommandHandler(
        IAccountRepository accountRepository,
        ILogger<CloseAccountCommandHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository, "accountRepository");
        Guard.AgainstNull(logger, "logger");

        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CloseAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _logger.LogInformation("Closing account {AccountId}", request.AccountId);

            var account = await _accountRepository.GetByIdAsync(
                request.AccountId,
                cancellationToken
            );
            if (account is null)
            {
                _logger.LogWarning("Account {AccountId} not found for closure", request.AccountId);
                return Result.Failure("Account not found");
            }

            // Close the account
            var closeResult = account.Close(request.Reason);
            if (!closeResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to close account {AccountId}: {Error}",
                    request.AccountId,
                    closeResult.Error
                );
                return closeResult;
            }

            await _accountRepository.UpdateAsync(account, cancellationToken);

            _logger.LogInformation("Account {AccountId} closed successfully", request.AccountId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing account {AccountId}", request.AccountId);
            return Result.Failure($"Error closing account: {ex.Message}");
        }
    }
}
