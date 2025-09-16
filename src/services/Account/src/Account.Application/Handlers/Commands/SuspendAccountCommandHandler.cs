using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Commands;

public class SuspendAccountCommandHandler : IRequestHandler<SuspendAccountCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<SuspendAccountCommandHandler> _logger;

    public SuspendAccountCommandHandler(
        IAccountRepository accountRepository,
        ILogger<SuspendAccountCommandHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository);
        Guard.AgainstNull(logger);

        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        SuspendAccountCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var account = await _accountRepository.GetByIdAsync(
                request.AccountId,
                cancellationToken
            );
            if (account is null)
            {
                _logger.LogWarning("Account {AccountId} not found.", request.AccountId);
                return Result.Failure("Account not found.");
            }

            var frozenResult = account.Suspend(request.Reason);
            if (!frozenResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to suspend account {AccountId}: {Error}",
                    request.AccountId,
                    frozenResult.Error
                );
                return frozenResult;
            }

            await _accountRepository.UpdateAsync(account, cancellationToken);
            _logger.LogInformation(
                "Account {AccountId} suspended successfully.",
                request.AccountId
            );
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending account {AccountId}", request.AccountId);
            return Result.Failure($"Error suspending account: {ex.Message}");
        }
    }
}
