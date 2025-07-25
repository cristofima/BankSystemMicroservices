using AutoMapper;
using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Handlers.Queries;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Application.Queries;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Handlers.Queries;

public class GetAccountByIdQueryHandlerTests
{
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetAccountByIdQueryHandler _handler;

    public GetAccountByIdQueryHandlerTests()
    {
        var mockLogger = new Mock<ILogger<GetAccountByIdQueryHandler>>();
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetAccountByIdQueryHandler(
            _mockAccountRepository.Object,
            _mockMapper.Object,
            mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ReturnsAccount_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = AccountEntity.CreateNew(Guid.NewGuid(), AccountType.Checking, Currency.USD, "test");
        var accountDto = new AccountDto();
        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _mockMapper.Setup(m => m.Map<AccountDto>(account)).Returns(accountDto);

        var query = new GetAccountByIdQuery(accountId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(accountDto, result.Value);
        _mockAccountRepository.Verify(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<AccountDto>(account), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountEntity?)null!);
        var query = new GetAccountByIdQuery(accountId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal($"Account {accountId} not found", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        _mockAccountRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));
        var query = new GetAccountByIdQuery(accountId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("An error occurred while retrieving the account", result.Error);
    }
}