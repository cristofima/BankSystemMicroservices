using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.Validation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Commands;

public class FreezeAccountCommandHandler : IRequestHandler<FreezeAccountCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<FreezeAccountCommandHandler> _logger;

    public FreezeAccountCommandHandler(
        IAccountRepository accountRepository,
        ILogger<FreezeAccountCommandHandler> logger
    )
    {
        Guard.AgainstNull(accountRepository);
        Guard.AgainstNull(logger);

        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<Result> Handle(
        FreezeAccountCommand request,
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

            var frozenResult = account.Freeze(request.Reason);
            if (!frozenResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Failed to freeze account {AccountId}: {Error}",
                    request.AccountId,
                    frozenResult.Error
                );
                return frozenResult;
            }

            await _accountRepository.UpdateAsync(account, cancellationToken);
            _logger.LogInformation("Account {AccountId} frozen successfully.", request.AccountId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error freezing account {AccountId}", request.AccountId);
            return Result.Failure($"Error freezing account: {ex.Message}");
        }
    }
}
