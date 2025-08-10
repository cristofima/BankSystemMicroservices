using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Validators;

namespace BankSystem.Account.Application.UnitTests.Validators;

public class FreezeAccountCommandValidatorTests
{
    private readonly FreezeAccountCommandValidator _commandValidator = new();

    [Fact]
    public async Task Handle_EmptyValues_ShouldReturnError()
    {
        // Arrange
        var accountId = Guid.Empty;
        var reason = string.Empty;
        var command = new FreezeAccountCommand(accountId, reason);

        // Act
        var validationResult = await _commandValidator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().HaveCount(2);
        validationResult.Errors.Should().Contain(e => e.ErrorMessage == "Account ID is required");
        validationResult
            .Errors.Should()
            .Contain(e => e.ErrorMessage == "Reason for freezing the account is required");
    }
}
