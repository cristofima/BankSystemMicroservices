using BankSystem.Account.Application.DTOs;
using BankSystem.Account.Application.Handlers.Queries;
using BankSystem.Account.Application.Interfaces;
using BankSystem.Account.Application.Queries;
using BankSystem.Account.Domain.Enums;
using BankSystem.Shared.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using AccountEntity = BankSystem.Account.Domain.Entities.Account;

namespace BankSystem.Account.Application.UnitTests.Handlers.Queries;

public class GetAccountsByCustomerIdQueryHandlerTests
{
    private readonly Mock<IAccountRepository> _mockAccountRepository;
    private readonly Mock<ICurrentUser> _mockCurrentUser;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetAccountsByCustomerIdQueryHandler _handler;
    private const string TestUserName = "testuser";

    public GetAccountsByCustomerIdQueryHandlerTests()
    {
        _mockAccountRepository = new Mock<IAccountRepository>();
        _mockCurrentUser = new Mock<ICurrentUser>();
        _mockMapper = new Mock<IMapper>();
        var mockLogger = new Mock<ILogger<GetAccountsByCustomerIdQueryHandler>>();

        _mockCurrentUser.Setup(s => s.CustomerId).Returns(Guid.NewGuid());
        _mockCurrentUser.Setup(s => s.UserName).Returns(TestUserName);

        _handler = new GetAccountsByCustomerIdQueryHandler(
            _mockAccountRepository.Object,
            _mockCurrentUser.Object,
            _mockMapper.Object,
            mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ReturnsAccounts_WhenAccountsExist()
    {
        // Arrange
        var customerId = _mockCurrentUser.Object.CustomerId;
        var mockAccount = AccountEntity.CreateNew(customerId, AccountType.Savings, Currency.USD);
        var accounts = new List<AccountEntity> { mockAccount, mockAccount };
        var accountDtos = new List<AccountDto> { new(), new() };
        _mockAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        _mockMapper.Setup(m => m.Map<IEnumerable<AccountDto>>(accounts)).Returns(accountDtos);
        var query = new GetAccountsByCustomerIdQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(accountDtos, result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenNoAccountsExist()
    {
        // Arrange
        var customerId = _mockCurrentUser.Object.CustomerId;
        var accounts = new List<AccountEntity>();
        var accountDtos = new List<AccountDto>();
        _mockAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        _mockMapper.Setup(m => m.Map<IEnumerable<AccountDto>>(accounts)).Returns(accountDtos);
        var query = new GetAccountsByCustomerIdQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        var customerId = _mockCurrentUser.Object.CustomerId;
        _mockAccountRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("db error"));
        var query = new GetAccountsByCustomerIdQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("error", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
