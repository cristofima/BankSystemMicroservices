using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Validators;
using BankSystem.Account.Domain.Enums;

namespace BankSystem.Account.Application.UnitTests.Validators;

public class CreateAccountCommandValidatorTests
{
    private readonly CreateAccountCommandValidator _commandValidator = new();

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType.Checking, Currency.EUR);

        // Act
        var validationResult = await _commandValidator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeTrue();
        validationResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InvalidCurrencyCode_ShouldReturnError()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType.Checking, "ABC");

        // Act
        var validationResult = await _commandValidator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult
            .Errors.Should()
            .ContainSingle(e => e.ErrorMessage == "Invalid currency code");
    }
}
