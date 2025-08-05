using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Domain.Enums;
using BankSystem.Account.Infrastructure.IntegrationTests.Common;
using BankSystem.Shared.Domain.ValueObjects;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Infrastructure.IntegrationTests.Repositories;

public class AccountRepositoryTests : BaseAccountInfrastructureTests
{
    private IAccountRepository GetAccountRepository()
    {
        return GetService<IAccountRepository>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAccount_WhenAccountExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var account = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD);

        var accountRepository = GetAccountRepository();

        await accountRepository.AddAsync(account);

        // Act
        var result = await accountRepository.GetByIdAsync(account.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.Id);
        Assert.Equal(account.CustomerId, result.CustomerId);
        Assert.Equal(account.Type, result.Type);
        Assert.Equal(account.AccountNumber, result.AccountNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountRepository = GetAccountRepository();
        var nonExistentAccountId = Guid.NewGuid();

        // Act
        var result = await accountRepository.GetByIdAsync(nonExistentAccountId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnAccounts_WhenCustomerHasAccounts()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var account1 = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD);

        var account2 = AccountEntity.CreateNew(customerId, AccountType.Checking, Currency.EUR);

        var accountRepository = GetAccountRepository();
        await accountRepository.AddAsync(account1);
        await accountRepository.AddAsync(account2);

        // Act
        var accounts = await accountRepository.GetByCustomerIdAsync(customerId);

        // Assert
        Assert.NotEmpty(accounts!);
        Assert.Contains(accounts!, a => a.Id == account1.Id);
        Assert.Contains(accounts!, a => a.Id == account2.Id);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnEmpty_WhenCustomerHasNoAccounts()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var accountRepository = GetAccountRepository();

        // Act
        var account = await accountRepository.GetByCustomerIdAsync(customerId);

        // Assert
        Assert.Empty(account);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAccount_WhenAccountExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var account = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD);

        var accountRepository = GetAccountRepository();
        await accountRepository.AddAsync(account);

        // Act
        account.Activate();
        await accountRepository.UpdateAsync(account);
        var result = await accountRepository.GetByIdAsync(account.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AccountStatus.Active, result.Status);
    }

    [Fact]
    public async Task AccountNumberExistsAsync_ShouldReturnTrue_WhenAccountNumberExists()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var account = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD);
        var accountRepository = GetAccountRepository();
        await accountRepository.AddAsync(account);

        // Act
        var exists = await accountRepository.AccountNumberExistsAsync(account.AccountNumber);

        // Assert
        Assert.True(exists);
    }
}
