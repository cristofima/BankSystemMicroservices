using BankSystem.Account.Application.Commands;
using BankSystem.Shared.Application.Extensions;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class SuspendAccountCommandValidator : AbstractValidator<SuspendAccountCommand>
{
    public SuspendAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmptyGuid().WithMessage("Account ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason for suspending the account is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}
