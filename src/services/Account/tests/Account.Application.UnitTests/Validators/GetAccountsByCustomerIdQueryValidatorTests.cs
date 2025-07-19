using BankSystem.Account.Application.Queries;
using BankSystem.Account.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BankSystem.Account.Application.UnitTests.Validators;

public class GetAccountsByCustomerIdQueryValidatorTests
{
    private readonly GetAccountsByCustomerIdQueryValidator _validator = new();

    [Fact]
    public void ShouldNot_HaveError_WhenCustomerIdIsValid()
    {
        // Arrange
        var query = new GetAccountsByCustomerIdQuery(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void Should_HaveError_WhenCustomerIdIsEmpty()
    {
        // Arrange
        var query = new GetAccountsByCustomerIdQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
            .WithErrorMessage("Customer ID is required");
    }
}