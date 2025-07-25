using BankSystem.Account.Application.Commands;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");
    }
}