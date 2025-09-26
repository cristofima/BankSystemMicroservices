using BankSystem.Account.Application.Queries;
using BankSystem.Shared.Application.Extensions;
using FluentValidation;

namespace BankSystem.Account.Application.Validators;

public class GetAccountByIdQueryValidator : AbstractValidator<GetAccountByIdQuery>
{
    public GetAccountByIdQueryValidator()
    {
        RuleFor(x => x.AccountId).NotEmptyGuid().WithMessage("Account ID is required");
    }
}
