using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Validators;

namespace BankSystem.Account.Application.UnitTests.Validators;

public class CloseAccountCommandValidatorTests
{
    private readonly CloseAccountCommandValidator _commandValidator = new();

    [Fact]
    public async Task Handle_EmptyReason_ShouldReturnError()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var reason = string.Empty;
        var command = new CloseAccountCommand(accountId, reason);

        // Act
        var validationResult = await _commandValidator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult
            .Errors.Should()
            .ContainSingle(e => e.ErrorMessage == "Reason for closing the account is required");
    }

    [Fact]
    public async Task Handle_EmptyAccountNumber_ShouldReturnError()
    {
        // Arrange
        const string reason = "Customer request";
        var command = new CloseAccountCommand(Guid.Empty, reason);

        // Act
        var validationResult = await _commandValidator.ValidateAsync(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult
            .Errors.Should()
            .ContainSingle(e => e.ErrorMessage == "Account ID is required");
    }
}
