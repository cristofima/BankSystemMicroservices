using FluentValidation;
using BankSystem.Account.Application.Queries;

namespace BankSystem.Account.Application.Validators;

public class GetAccountsByCustomerIdQueryValidator : AbstractValidator<GetAccountsByCustomerIdQuery>
{
    public GetAccountsByCustomerIdQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");
    }
}
