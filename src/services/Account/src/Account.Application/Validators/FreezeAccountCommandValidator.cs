using BankSystem.Account.Application.Commands;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class FreezeAccountCommandValidator : AbstractValidator<FreezeAccountCommand>
{
    public FreezeAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason for freezing the account is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}