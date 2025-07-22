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

public class SuspendAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<IAuthenticatedUserService> _mockAuthenticatedUserService;
    private readonly SuspendAccountCommandHandler _handler;
    private readonly string userName = "testuser";

    public SuspendAccountCommandHandlerTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockAuthenticatedUserService = new Mock<IAuthenticatedUserService>();
        var mockLogger = new Mock<ILogger<SuspendAccountCommandHandler>>();

        _mockAuthenticatedUserService.Setup(s => s.CustomerId).Returns(Guid.NewGuid());
        _mockAuthenticatedUserService.Setup(s => s.UserName).Returns(userName);

        _handler = new SuspendAccountCommandHandler(
            _mockAccountRepository.Object,
            _mockAuthenticatedUserService.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenAccountExistsAndCanBeSuspended_ShouldReturnSuccess()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var customerId = _mockAuthenticatedUserService.Object.CustomerId;
        var command = new SuspendAccountCommand(accountId, "Suspicious activity");
        var mockAccount = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD, userName);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAccount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        command.ValidationErrorTitle.Should().NotBeNullOrEmpty();
        result.IsSuccess.Should().BeTrue();
        _mockAccountRepository.Verify(r => r.UpdateAsync(mockAccount, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new SuspendAccountCommand(accountId, "Reason");

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountEntity)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenAccountSuspendFails_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var customerId = _mockAuthenticatedUserService.Object.CustomerId;
        var command = new SuspendAccountCommand(accountId, "Reason");
        var mockAccount = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD, userName);
        mockAccount.Close("Closing account", userName);

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockAccount);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closed");
        _mockAccountRepository.Verify(r => r.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new SuspendAccountCommand(accountId, "Reason");
        var exception = new Exception("Database error");

        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain(exception.Message);
    }
}