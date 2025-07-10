using AccountEntity = Account.Domain.Entities.Account;
using Account.Domain.Enums;
using Account.Domain.Events;
using Account.Domain.Exceptions;
using BankSystem.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace Account.Domain.UnitTests.Entities;

public class AccountTests
{
    [Fact]
    public void CreateNew_ValidParameters_ShouldCreateAccount()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var accountType = AccountType.Checking;
        var currency = Currency.USD;
        var initialDeposit = new Money(1000m, currency);

        // Act
        var account = AccountEntity.CreateNew(customerId, accountType, currency, initialDeposit);

        // Assert
        account.Should().NotBeNull();
        account.CustomerId.Should().Be(customerId);
        account.Type.Should().Be(accountType);
        account.Balance.Should().Be(initialDeposit);
        account.Status.Should().Be(AccountStatus.PendingActivation);
        account.DomainEvents.Should().ContainSingle(e => e is AccountCreatedEvent);
    }

    [Fact]
    public void Activate_ValidAccount_ShouldActivateAccount()
    {
        // Arrange
        var currency = Currency.USD;
        var initialDeposit = new Money(1000m, currency);
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, currency, initialDeposit);

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
        var currency = Currency.USD;
        var initialDeposit = new Money(1000m, currency);
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, currency, initialDeposit);
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
        var currency = Currency.USD;
        var initialDeposit = new Money(1000m, currency);
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, currency, initialDeposit);
        account.Activate();
        account.Withdraw(new Money(1000m, currency), "Test");

        // Act
        account.Close("Customer request");

        // Assert
        account.Status.Should().Be(AccountStatus.Closed);
        account.DomainEvents.Should().ContainSingle(e => e is AccountClosedEvent);
    }

    [Fact]
    public void Deposit_ValidAmount_ShouldIncreaseBalance()
    {
        // Arrange
        var currency = Currency.USD;
        var initialDeposit = new Money(0m, currency);
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, currency, initialDeposit);
        account.Activate();
        var depositAmount = new Money(500m, Currency.USD);

        // Act
        var result = account.Deposit(depositAmount, "Test deposit");

        // Assert
        account.Balance.Amount.Should().Be(depositAmount.Amount);
        result.IsSuccess.Should().BeTrue();
        account.DomainEvents.Should().ContainSingle(e => e is MoneyDepositedEvent);
    }

    [Fact]
    public void Withdraw_ValidAmount_ShouldDecreaseBalance()
    {
        // Arrange
        var currency = Currency.USD;
        var initialDeposit = new Money(1000m, currency);
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, currency, initialDeposit);
        account.Activate();
        var withdrawAmount = new Money(500m, Currency.USD);

        // Act
        var transaction = account.Withdraw(withdrawAmount, "Test withdrawal");

        // Assert
        account.Balance.Amount.Should().Be(500m);
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(withdrawAmount);
        account.DomainEvents.Should().ContainSingle(e => e is MoneyWithdrawnEvent);
    }

    [Fact]
    public void Withdraw_InsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var currency = Currency.USD;
        var initialDeposit = new Money(100m, currency);
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, currency, initialDeposit);
        account.Activate();
        var withdrawAmount = new Money(200m, Currency.USD);

        // Act
        Action action = () => account.Withdraw(withdrawAmount, "Test withdrawal");

        // Assert
        action.Should().Throw<InsufficientFundsException>();
    }
}
