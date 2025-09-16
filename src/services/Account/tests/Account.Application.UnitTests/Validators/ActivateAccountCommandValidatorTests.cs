using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Validators;

namespace BankSystem.Account.Application.UnitTests.Validators;

public class ActivateAccountCommandValidatorTests
{
    private readonly ActivateAccountCommandValidator _commandValidator = new();

    [Fact]
    public async Task Handle_EmptyAccountId_ShouldReturnError()
    {
        // Arrange
        var accountId = Guid.Empty;
        var command = new ActivateAccountCommand(accountId);

        // Act
        var validationResult = await _commandValidator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult
            .Errors.Should()
            .ContainSingle(e => e.ErrorMessage == "Account ID is required");
    }
}
