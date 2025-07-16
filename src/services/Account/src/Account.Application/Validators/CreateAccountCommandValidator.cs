using BankSystem.Account.Application.Commands;
using BankSystem.Shared.Domain.ValueObjects;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.AccountType)
            .IsInEnum()
            .WithMessage("Invalid account type");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be 3 characters")
            .Must(BeValidCurrencyCode)
            .WithMessage("Invalid currency code");
    }

    private static bool BeValidCurrencyCode(string currency)
    {
        return Currency.IsValidCurrencyCode(currency);
    }
}
