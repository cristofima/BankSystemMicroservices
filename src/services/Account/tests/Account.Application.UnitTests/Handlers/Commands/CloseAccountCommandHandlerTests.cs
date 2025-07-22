using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Handlers.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Handlers.Commands;

public class CloseAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<IAuthenticatedUserService> _mockAuthenticatedUserService;
    private readonly CloseAccountCommandHandler _handler;
    private readonly string userName = "testuser";

    public CloseAccountCommandHandlerTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockAuthenticatedUserService = new Mock<IAuthenticatedUserService>();
        var mockLogger = new Mock<ILogger<CloseAccountCommandHandler>>();
        _mockAuthenticatedUserService.Setup(s => s.CustomerId).Returns(Guid.NewGuid());
        _mockAuthenticatedUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _mockAuthenticatedUserService.Setup(s => s.UserName).Returns(userName);

        _handler = new CloseAccountCommandHandler(_mockAccountRepository.Object, _mockAuthenticatedUserService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidActiveAccount_ShouldCloseAccountSuccessfully()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var customerId = _mockAuthenticatedUserService.Object.CustomerId;
        const string reason = "Customer request";

        var command = new CloseAccountCommand(accountId, reason);

        var account = AccountEntity.CreateNew(
            customerId,
            AccountType.Checking,
            Currency.USD,
            userName);

        _mockAccountRepository
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _mockAccountRepository
            .Setup(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        command.ValidationErrorTitle().Should().NotBeNullOrEmpty();
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        account.Status.Should().Be(AccountStatus.Closed);

        _mockAccountRepository.Verify(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateAsync(It.Is<AccountEntity>(a => a.Status == AccountStatus.Closed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        const string reason = "Customer request";
        var command = new CloseAccountCommand(accountId, reason);

        _mockAccountRepository
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountEntity?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account not found");

        _mockAccountRepository.Verify(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyClosedAccount_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var customerId = _mockAuthenticatedUserService.Object.CustomerId;
        const string reason = "Customer request";

        var command = new CloseAccountCommand(accountId, reason);

        var account = AccountEntity.CreateNew(
            customerId,
            AccountType.Checking,
            Currency.USD,
            userName);

        account.Close(reason, userName);

        _mockAccountRepository
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already closed");

        _mockAccountRepository.Verify(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ZeroBalanceAccount_ShouldCloseSuccessfully()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var customerId = _mockAuthenticatedUserService.Object.CustomerId;
        const string reason = "Customer request";

        var command = new CloseAccountCommand(accountId, reason);

        var account = AccountEntity.CreateNew(
            customerId,
            AccountType.Checking,
            Currency.USD,
            userName);

        _mockAccountRepository
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _mockAccountRepository
            .Setup(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        account.Status.Should().Be(AccountStatus.Closed);

        _mockAccountRepository.Verify(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateAsync(It.Is<AccountEntity>(a => a.Status == AccountStatus.Closed), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new CloseAccountCommand(accountId, "Customer request");

        _mockAccountRepository
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Database error");

        _mockAccountRepository.Verify(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateAsyncThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var customerId = _mockAuthenticatedUserService.Object.CustomerId;
        const string reason = "Customer request";

        var command = new CloseAccountCommand(accountId, reason);

        var account = AccountEntity.CreateNew(
            customerId,
            AccountType.Checking,
            Currency.USD,
            userName);

        _mockAccountRepository
            .Setup(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _mockAccountRepository
            .Setup(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Database error");

        _mockAccountRepository.Verify(x => x.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CancellationTokenCancelled_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new CloseAccountCommand(accountId, "Customer request");

        var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        // Act
        var result = await _handler.Handle(command, cancellationTokenSource.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
    }
}