using BankSystem.Account.Application.Commands;
using BankSystem.Account.Application.Handlers.Commands;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Application.Validators;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Handlers.Commands
{
    public class CloseAccountCommandHandlerTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly CloseAccountCommandHandler _handler;
        private readonly CloseAccountCommandValidator _commandValidator = new();

        public CloseAccountCommandHandlerTests()
        {
            _mockAccountRepository = new Mock<IAccountRepository>();
            var mockLogger = new Mock<ILogger<CloseAccountCommandHandler>>();
            _handler = new CloseAccountCommandHandler(_mockAccountRepository.Object, mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ValidActiveAccount_ShouldCloseAccountSuccessfully()
        {
            // Arrange
            const string accountNumber = "12345";
            var customerId = Guid.NewGuid();
            const string reason = "Customer request";

            var command = new CloseAccountCommand(accountNumber, reason);

            var account = AccountEntity.CreateNew(
                customerId,
                AccountType.Checking,
                Currency.USD,
                new Money(100m, Currency.USD));

            account.Activate();
            account.Withdraw(new Money(100m, Currency.USD), "Test Withdraw");

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
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

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.UpdateAsync(It.Is<AccountEntity>(a => a.Status == AccountStatus.Closed), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AccountNotFound_ShouldReturnFailure()
        {
            // Arrange
            const string accountNumber = "12345"; ;
            const string reason = "Customer request";
            var command = new CloseAccountCommand(accountNumber, reason);

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AccountEntity?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Account not found");

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AlreadyClosedAccount_ShouldReturnFailure()
        {
            // Arrange
            const string accountNumber = "12345";
            var customerId = Guid.NewGuid();
            const string reason = "Customer request";

            var command = new CloseAccountCommand(accountNumber, reason);

            var account = AccountEntity.CreateNew(
                customerId,
                AccountType.Checking,
                Currency.USD,
                new Money(0m, Currency.USD));

            account.Close(reason);

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("already closed");

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_AccountWitNonZeroBalance_ShouldReturnFailure()
        {
            // Arrange
            const string accountNumber = "12345";
            var customerId = Guid.NewGuid();
            const string reason = "Customer request";

            var command = new CloseAccountCommand(accountNumber, reason);

            var account = AccountEntity.CreateNew(
                customerId,
                AccountType.Checking,
                Currency.USD,
                new Money(100m, Currency.USD));

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("non-zero balance");

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ZeroBalanceAccount_ShouldCloseSuccessfully()
        {
            // Arrange
            const string accountNumber = "12345";
            var customerId = Guid.NewGuid();
            const string reason = "Customer request";

            var command = new CloseAccountCommand(accountNumber, reason);

            var account = AccountEntity.CreateNew(
                customerId,
                AccountType.Checking,
                Currency.USD,
                new Money(0m, Currency.USD));

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
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

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.UpdateAsync(It.Is<AccountEntity>(a => a.Status == AccountStatus.Closed), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Handle_NullOrEmptyReason_ShouldReturnError(string reason)
        {
            // Arrange
            const string accountNumber = "12345";
            var command = new CloseAccountCommand(accountNumber, reason);

            // Act
            var validationResult = await _commandValidator.ValidateAsync(command);

            // Assert
            validationResult.IsValid.Should().BeFalse();
            validationResult.Errors.Should().ContainSingle(e => e.ErrorMessage == "Reason for closing the account is required");
        }

        [Fact]
        public async Task Handle_EmptyAccountNumber_ShouldReturnError()
        {
            // Arrange
            const string reason = "Customer request";
            var command = new CloseAccountCommand(string.Empty, reason);

            // Act
            var validationResult = await _commandValidator.ValidateAsync(command);

            // Assert
            validationResult.IsValid.Should().BeFalse();
            validationResult.Errors.Should().ContainSingle(e => e.ErrorMessage == "Account Number is required");
        }

        [Fact]
        public async Task Handle_RepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            const string accountNumber = "12345";
            var command = new CloseAccountCommand(accountNumber, "Customer request");

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Contain("Database error");

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateAsyncThrowsException_ShouldReturnFailure()
        {
            // Arrange
            const string accountNumber = "12345";
            var customerId = Guid.NewGuid();
            const string reason = "Customer request";

            var command = new CloseAccountCommand(accountNumber, reason);

            var account = AccountEntity.CreateNew(
                customerId,
                AccountType.Checking,
                Currency.USD,
                new Money(0m, Currency.USD));

            _mockAccountRepository
                .Setup(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()))
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

            _mockAccountRepository.Verify(x => x.GetByAccountNumberAsync(accountNumber, It.IsAny<CancellationToken>()), Times.Once);
            _mockAccountRepository.Verify(x => x.UpdateAsync(It.IsAny<AccountEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CancellationTokenCancelled_ShouldReturnFailure()
        {
            // Arrange
            const string accountNumber = "12345";
            var command = new CloseAccountCommand(accountNumber, "Customer request");

            var cancellationTokenSource = new CancellationTokenSource();
            await cancellationTokenSource.CancelAsync();

            // Act
            var result = await _handler.Handle(command, cancellationTokenSource.Token);

            // Assert
            result.IsFailure.Should().BeTrue();
        }
    }
}