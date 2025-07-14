using FluentValidation;
using BankSystem.Account.Application.Queries;

namespace BankSystem.Account.Application.Validators;

public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    public GetAccountByIdQueryValidator()
    {
        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .WithMessage("Account Number is required");
    }
}
