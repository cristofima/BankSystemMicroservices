using FluentValidation;
using BankSystem.Account.Application.Commands;

namespace BankSystem.Account.Application.Validators;

public class CloseAccountCommandValidator : AbstractValidator<CloseAccountCommand>
{
    public CloseAccountCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .WithMessage("Account Number is required");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason for closing the account is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");
    }
}
