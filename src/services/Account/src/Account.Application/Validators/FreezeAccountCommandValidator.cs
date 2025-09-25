using BankSystem.Account.Application.Commands;
using BankSystem.Shared.Application.Extensions;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class FreezeAccountCommandValidator : AbstractValidator<FreezeAccountCommand>
{
    public FreezeAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmptyGuid().WithMessage("Account ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason for freezing the account is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}
