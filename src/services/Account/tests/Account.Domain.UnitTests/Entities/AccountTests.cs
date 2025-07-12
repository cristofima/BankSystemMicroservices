using AccountEntity = BankSystem.Account.Domain.Entities.Account;
using BankSystem.Account.Domain.Enums;
using BankSystem.Account.Domain.Events;
using BankSystem.Account.Domain.Exceptions;
using BankSystem.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace BankSystem.Account.Domain.UnitTests.Entities;

public class AccountTests
{
    private static AccountEntity CreateTestAccount(Guid customerId, AccountType accountType, decimal amount, Currency? currency = null)
    {
        currency ??= Currency.USD;
        var initialDeposit = new Money(amount, currency);
        return AccountEntity.CreateNew(customerId, accountType, currency, initialDeposit);
    }

    [Fact]
    public void CreateNew_ValidParameters_ShouldCreateAccount()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        const AccountType accountType = AccountType.Checking;
        const decimal amount = 3000m;

        // Act
        var account = CreateTestAccount(customerId, accountType, amount);

        // Assert
        account.Should().NotBeNull();
        account.CustomerId.Should().Be(customerId);
        account.Type.Should().Be(accountType);
        account.Balance.Amount.Should().Be(amount);
        account.Status.Should().Be(AccountStatus.PendingActivation);
        account.DomainEvents.Should().ContainSingle(e => e is AccountCreatedEvent);
    }

    [Fact]
    public void Activate_ValidAccount_ShouldActivateAccount()
    {
        // Arrange
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking, 100m);

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
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking, 1000m);
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
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking, 1000m, currency);
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
        var currency = Currency.GBP;
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Savings, 0m, currency);
        account.Activate();
        var depositAmount = new Money(500m, currency);

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
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Business, 1000m, currency);
        account.Activate();
        var withdrawAmount = new Money(500m, currency);

        // Act
        var result = account.Withdraw(withdrawAmount, "Test withdrawal");

        // Assert
        account.Balance.Amount.Should().Be(500m);
        result.Value.Should().NotBeNull();
        result.Value.Amount.Should().Be(withdrawAmount);
        account.DomainEvents.Should().ContainSingle(e => e is MoneyWithdrawnEvent);
    }

    [Fact]
    public void Withdraw_InsufficientFunds_ShouldThrowException()
    {
        // Arrange
        var currency = Currency.USD;
        var account = CreateTestAccount(Guid.NewGuid(), AccountType.Checking, 10m, currency);
        account.Activate();
        var withdrawAmount = new Money(200m, currency);

        // Act
        Action action = () => account.Withdraw(withdrawAmount, "Test withdrawal");

        // Assert
        action.Should().Throw<InsufficientFundsException>();
    }
}
