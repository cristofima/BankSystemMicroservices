using BankSystem.Account.Application.Queries;
using BankSystem.Account.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BankSystem.Account.Application.UnitTests.Validators;

public class GetAccountByIdQueryValidatorTests
{
    private readonly GetAccountByIdQueryValidator _validator = new();

    [Fact]
    public void ShouldNot_HaveError_WhenAccountIdIsValid()
    {
        // Arrange
        var query = new GetAccountByIdQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ShouldNotHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Should_HaveError_WhenAccountIdIsEmpty()
    {
        // Arrange
        var query = new GetAccountByIdQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AccountId)
            .WithErrorMessage("Account ID is required");
    }
}
