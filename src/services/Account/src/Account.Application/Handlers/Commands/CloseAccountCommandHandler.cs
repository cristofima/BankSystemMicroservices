using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BankSystem.Account.Application.Handlers.Commands;

public class CloseAccountCommandHandler : IRequestHandler<CloseAccountCommand, Result>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<CloseAccountCommandHandler> _logger;

    public CloseAccountCommandHandler(
        IAccountRepository accountRepository,
        ILogger<CloseAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Closing account {AccountNumber}", request.AccountNumber);

            var account = await _accountRepository.GetByAccountNumberAsync(request.AccountNumber, cancellationToken);
            if (account == null)
            {
                _logger.LogWarning("Account {AccountNumber} not found for closure", request.AccountNumber);
                return Result.Failure("Account not found");
            }

            // Close the account
            var closeResult = account.Close(request.Reason);
            if (!closeResult.IsSuccess)
            {
                _logger.LogWarning("Failed to close account {AccountNumber}: {Error}", request.AccountNumber, closeResult.Error);
                return closeResult;
            }

            await _accountRepository.UpdateAsync(account, cancellationToken);

            _logger.LogInformation("Account {AccountNumber} closed successfully", request.AccountNumber);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing account {AccountNumber}", request.AccountNumber);
            return Result.Failure($"Error closing account: {ex.Message}");
        }
    }
}
