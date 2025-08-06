using AutoMapper;
using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Handlers.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.Common;
using BankSystem.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Handlers.Commands;

public class CreateAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<CreateAccountCommandHandler>> _mockLogger;
    private readonly CreateAccountCommandHandler _handler;

    public CreateAccountCommandHandlerTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<CreateAccountCommandHandler>>();

        _mockCurrentUser.Setup(s => s.CustomerId).Returns(Guid.NewGuid());
        _mockCurrentUser.Setup(s => s.UserId).Returns(Guid.NewGuid());

        _handler = new CreateAccountCommandHandler(
            _mockAccountRepository.Object,
            _mockCurrentUser.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldReturnSuccessResult()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType.Checking, "USD");

        var currency = new Currency(command.Currency);
        var expectedAccount = AccountEntity.CreateNew(
            _mockCurrentUser.Object.CustomerId,
            command.AccountType,
            currency
        );

        var expectedDto = new AccountDto
        {
            Id = expectedAccount.Id,
            CustomerId = expectedAccount.CustomerId,
            AccountNumber = expectedAccount.AccountNumber.Value,
            Balance = expectedAccount.Balance.Amount,
            Currency = expectedAccount.Balance.Currency.Code,
            Status = expectedAccount.Status.ToString(),
            AccountType = expectedAccount.Type.ToString(),
            CreatedAt = expectedAccount.CreatedAt,
        };

        _mockAccountRepository
            .Setup(r => r.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<AccountDto>(It.IsAny<AccountEntity>())).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        command.ValidationErrorTitle().Should().NotBeNullOrEmpty();
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Currency, result.Value.Currency);
        Assert.Equal(command.AccountType.ToString(), result.Value.AccountType);

        _mockAccountRepository.Verify(
            r => r.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Theory]
    [InlineData(AccountType.Checking, "USD")]
    [InlineData(AccountType.Savings, "EUR")]
    [InlineData(AccountType.Business, "GBP")]
    public async Task Handle_DifferentAccountTypesAndCurrencies_ShouldCreateAccountSuccessfully(
        AccountType accountType,
        string currency
    )
    {
        // Arrange
        var command = new CreateAccountCommand(accountType, currency);

        var expectedDto = new AccountDto
        {
            Id = Guid.NewGuid(),
            CustomerId = _mockCurrentUser.Object.CustomerId,
            AccountNumber = "1234567890",
            Balance = 0,
            Currency = currency,
            Status = nameof(AccountStatus.Active),
            AccountType = accountType.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        _mockAccountRepository
            .Setup(r => r.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(m => m.Map<AccountDto>(It.IsAny<AccountEntity>())).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(accountType.ToString(), result.Value!.AccountType);
        Assert.Equal(currency, result.Value.Currency);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateAccountSuccessfully()
    {
        // Arrange
        var customerId = _mockCurrentUser.Object.CustomerId;
        var command = new CreateAccountCommand(AccountType: AccountType.Checking, Currency: "USD");

        var createdAccount = AccountEntity.CreateNew(
            customerId,
            AccountType.Checking,
            new Currency("USD")
        );

        var expectedDto = new AccountDto
        {
            Id = createdAccount.Id,
            AccountNumber = createdAccount.AccountNumber,
            CustomerId = customerId,
            AccountType = "Checking",
            Balance = 1000m,
        };

        _mockAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(x => x.Map<AccountDto>(It.IsAny<AccountEntity>())).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CustomerId.Should().Be(customerId);
        result.Value!.AccountType.Should().Be("Checking");
        result.Value!.Balance.Should().Be(1000m);

        _mockAccountRepository.Verify(
            x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockMapper.Verify(x => x.Map<AccountDto>(It.IsAny<AccountEntity>()), Times.Once);

        VerifyLoggerWasCalled(LogLevel.Information, "Creating account for customer");
    }

    [Fact]
    public async Task Handle_ValidCommandWithZeroBalance_ShouldCreateAccountWithoutDeposit()
    {
        // Arrange
        var customerId = _mockCurrentUser.Object.CustomerId;
        var command = new CreateAccountCommand(AccountType: AccountType.Savings, Currency: "EUR");

        var expectedDto = new AccountDto
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1234567890",
            CustomerId = customerId,
            AccountType = "Savings",
            Balance = 0m,
        };

        _mockAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper.Setup(x => x.Map<AccountDto>(It.IsAny<AccountEntity>())).Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CustomerId.Should().Be(customerId);
        result.Value.AccountType.Should().Be("Savings");
        result.Value.Balance.Should().Be(0m);

        _mockAccountRepository.Verify(
            x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()),
            Times.Once
        );

        VerifyLoggerWasCalled(LogLevel.Information, "Creating account for customer");
    }

    [Fact]
    public async Task Handle_ExceptionThrown_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType: AccountType.Checking, Currency: "USD");

        _mockAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("error occurred while creating the account");

        VerifyLoggerWasCalled(LogLevel.Error, "Error creating account for customer");
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldLogErrorAndReturnFailure()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType: AccountType.Checking, Currency: "USD");

        var expectedException = new Exception("Repository failed");

        _mockAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("error occurred while creating the account");
    }

    [Fact]
    public async Task Handle_MapperThrowsException_ShouldLogErrorAndReturnFailure()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType: AccountType.Checking, Currency: "USD");

        var expectedException = new AutoMapperMappingException("Mapping failed");

        _mockAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMapper
            .Setup(x => x.Map<AccountDto>(It.IsAny<AccountEntity>()))
            .Throws(expectedException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("error occurred while creating the account");
    }

    [Fact]
    public async Task Handle_CancellationRequested_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateAccountCommand(AccountType: AccountType.Checking, Currency: "USD");

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _mockAccountRepository
            .Setup(x => x.AddAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _handler.Handle(command, cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    private void VerifyLoggerWasCalled(LogLevel logLevel, string message)
    {
        _mockLogger.Verify(
            x =>
                x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.AtLeastOnce
        );
    }
}
