using BankSystem.Account.Application.Commands;
using BankSystem.Shared.Application.Extensions;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmptyGuid().WithMessage("Account ID is required");
    }
}
