using AccountEntity = BankSystem.Account.Domain.Entities.Account;
using BankSystem.Account.Domain.Enums;
using BankSystem.Account.Domain.Events;
using BankSystem.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace BankSystem.Account.Domain.UnitTests.Entities;

public class AccountTests
{
    private static AccountEntity CreateTestAccount(Guid customerId, AccountType accountType, Currency? currency = null)
    {
        currency ??= Currency.USD;
        return AccountEntity.CreateNew(customerId, accountType, currency);
    }

    [Fact]
    public void CreateNew_ValidParameters_ShouldCreateAccount()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        const AccountType accountType = AccountType.Checking;

        // Act
        var account = CreateTestAccount(customerId, accountType);

        // Assert
        account.Should().NotBeNull();
        account.CustomerId.Should().Be(customerId);
        account.Type.Should().Be(accountType);
        account.Balance.Amount.Should().Be(0);
        account.Status.Should().Be(AccountStatus.PendingActivation);
        account.DomainEvents.Should().ContainSingle(e => e is AccountCreatedEvent);
    }

    [Fact]
    public void Activate_ValidAccount_ShouldActivateAccount()
    {
        // Arrange
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking);

        // Act
        account.Activate();

        // Assert
        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle(e => e is AccountActivatedEvent);
    }

    [Fact]
    public void Suspend_ValidAccount_ShouldSuspendAccount()
    {
        // Arrange
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking);
        account.Activate();

        // Act
        account.Suspend("Suspicious activity");

        // Assert
        account.Status.Should().Be(AccountStatus.Suspended);
        account.DomainEvents.Should().ContainSingle(e => e is AccountSuspendedEvent);
    }

    [Fact]
    public void Close_ValidAccount_ShouldCloseAccount()
    {
        // Arrange
        var currency = Currency.EUR;
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking, currency);
        account.Activate();

        // Act
        account.Close("Customer request");

        // Assert
        account.Status.Should().Be(AccountStatus.Closed);
        account.DomainEvents.Should().ContainSingle(e => e is AccountClosedEvent);
    }
}
