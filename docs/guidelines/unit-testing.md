# Unit Testing Guidelines

## Overview

This document provides comprehensive guidelines for writing effective unit tests in the Bank System Microservices project. These guidelines focus on testing domain logic, application services, and API controllers using modern .NET testing frameworks.

## Testing Framework Setup

### Test Project Configuration

```xml
<!-- Test project file (.csproj) -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NUnit" Version="4.0.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="AutoFixture" Version="4.18.1" />
    <PackageReference Include="AutoFixture.NUnit3" Version="4.18.1" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\BankSystem.Account.Domain\BankSystem.Account.Domain.csproj" />
    <ProjectReference Include="..\..\src\BankSystem.Account.Application\BankSystem.Account.Application.csproj" />
    <ProjectReference Include="..\..\src\BankSystem.Account.Api\BankSystem.Account.Api.csproj" />
  </ItemGroup>

</Project>
```

### Test Base Classes

```csharp
// Base test class for unit tests
public abstract class TestBase
{
    protected IFixture Fixture { get; private set; } = null!;
    protected Mock<ILogger> MockLogger { get; private set; } = null!;

    [SetUp]
    public virtual void SetUp()
    {
        Fixture = new Fixture();
        Fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        MockLogger = new Mock<ILogger>();
    }

    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }

    protected T CreateEntity<T>() where T : class
    {
        return Fixture.Create<T>();
    }

    protected List<T> CreateMany<T>(int count = 3) where T : class
    {
        return Fixture.CreateMany<T>(count).ToList();
    }
}

// Base test class for domain tests
public abstract class DomainTestBase : TestBase
{
    protected Money CreateMoney(decimal amount = 1000m, string currencyCode = "USD")
    {
        return new Money(amount, Currency.FromCode(currencyCode));
    }

    protected Account CreateTestAccount(
        string accountNumber = "1234567890",
        Guid? customerId = null,
        decimal initialBalance = 1000m)
    {
        return Account.CreateNew(
            accountNumber,
            customerId ?? Guid.NewGuid(),
            CreateMoney(initialBalance));
    }
}
```

## Domain Entity Testing

### Testing Value Objects

```csharp
// ✅ Good: Comprehensive value object tests
[TestFixture]
public class MoneyTests : DomainTestBase
{
    [Test]
    public void Constructor_ValidAmount_ShouldCreateMoney()
    {
        // Arrange
        var amount = 100.50m;
        var currency = Currency.USD;

        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }

    [Test]
    public void Constructor_NegativeAmount_ShouldThrowException()
    {
        // Arrange
        var amount = -100m;
        var currency = Currency.USD;

        // Act & Assert
        var action = () => new Money(amount, currency);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*");
    }

    [Test]
    public void Add_SameCurrency_ShouldReturnCorrectSum()
    {
        // Arrange
        var money1 = new Money(100m, Currency.USD);
        var money2 = new Money(50m, Currency.USD);

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.Should().Be(150m);
        result.Currency.Should().Be(Currency.USD);
    }

    [Test]
    public void Add_DifferentCurrency_ShouldThrowException()
    {
        // Arrange
        var usdMoney = new Money(100m, Currency.USD);
        var eurMoney = new Money(50m, Currency.EUR);

        // Act & Assert
        var action = () => usdMoney.Add(eurMoney);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add EUR to USD*");
    }

    [TestCase(100, 50, true)]
    [TestCase(50, 100, false)]
    [TestCase(100, 100, false)]
    public void GreaterThan_DifferentAmounts_ShouldReturnExpectedResult(
        decimal amount1, decimal amount2, bool expected)
    {
        // Arrange
        var money1 = new Money(amount1, Currency.USD);
        var money2 = new Money(amount2, Currency.USD);

        // Act
        var result = money1 > money2;

        // Assert
        result.Should().Be(expected);
    }

    [Test]
    public void Equals_SameAmountAndCurrency_ShouldBeEqual()
    {
        // Arrange
        var money1 = new Money(100m, Currency.USD);
        var money2 = new Money(100m, Currency.USD);

        // Act & Assert
        money1.Should().Be(money2);
        (money1 == money2).Should().BeTrue();
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }
}

// ❌ Bad: Insufficient value object tests
[TestFixture]
public class MoneyTestsBad
{
    [Test]
    public void Constructor_ShouldWork()
    {
        var money = new Money(100, Currency.USD);
        Assert.That(money.Amount, Is.EqualTo(100)); // Only basic test
    }
}
```

### Testing Domain Entities

```csharp
// ✅ Good: Comprehensive entity tests
[TestFixture]
public class AccountTests : DomainTestBase
{
    private Account _account;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _account = CreateTestAccount();
    }

    [Test]
    public void CreateNew_ValidParameters_ShouldCreateAccount()
    {
        // Arrange
        var accountNumber = "1234567890";
        var customerId = Guid.NewGuid();
        var initialDeposit = CreateMoney(500m);

        // Act
        var account = Account.CreateNew(accountNumber, customerId, initialDeposit);

        // Assert
        account.Id.Should().NotBeEmpty();
        account.AccountNumber.Should().Be(accountNumber);
        account.CustomerId.Should().Be(customerId);
        account.Balance.Should().Be(initialDeposit);
        account.Status.Should().Be(AccountStatus.Active);
        account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AccountCreatedEvent>();
    }

    [Test]
    public void Deposit_ValidAmount_ShouldIncreaseBalance()
    {
        // Arrange
        var depositAmount = CreateMoney(200m);
        var initialBalance = _account.Balance;
        var description = "Test deposit";

        // Act
        var result = _account.Deposit(depositAmount, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _account.Balance.Should().Be(initialBalance.Add(depositAmount));
        _account.Transactions.Should().ContainSingle(t =>
            t.Type == TransactionType.Deposit &&
            t.Amount.Equals(depositAmount) &&
            t.Description == description);
        _account.DomainEvents.Should().Contain(e => e is MoneyDepositedEvent);
    }

    [Test]
    public void Withdraw_SufficientFunds_ShouldDecreaseBalance()
    {
        // Arrange
        var withdrawAmount = CreateMoney(300m);
        var initialBalance = _account.Balance;
        var description = "Test withdrawal";

        // Act
        var result = _account.Withdraw(withdrawAmount, description);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _account.Balance.Should().Be(initialBalance.Subtract(withdrawAmount));
        _account.Transactions.Should().ContainSingle(t =>
            t.Type == TransactionType.Withdrawal &&
            t.Amount.Equals(withdrawAmount));
        _account.DomainEvents.Should().Contain(e => e is MoneyWithdrawnEvent);
    }

    [Test]
    public void Withdraw_InsufficientFunds_ShouldFail()
    {
        // Arrange
        var withdrawAmount = CreateMoney(2000m); // More than balance
        var initialBalance = _account.Balance;
        var description = "Test withdrawal";

        // Act
        var result = _account.Withdraw(withdrawAmount, description);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Insufficient funds");
        _account.Balance.Should().Be(initialBalance); // Balance unchanged
        _account.Transactions.Should().BeEmpty(); // No transaction created
        _account.DomainEvents.Should().BeEmpty(); // No events published
    }

    [Test]
    public void Withdraw_InactiveAccount_ShouldFail()
    {
        // Arrange
        _account.Freeze("Test freeze");
        var withdrawAmount = CreateMoney(100m);
        _account.ClearDomainEvents(); // Clear freeze events

        // Act
        var result = _account.Withdraw(withdrawAmount, "Test withdrawal");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Account is not active");
    }

    [TestCase(0)]
    [TestCase(-100)]
    public void Withdraw_InvalidAmount_ShouldFail(decimal amount)
    {
        // Arrange
        var withdrawAmount = CreateMoney(amount);

        // Act
        var result = _account.Withdraw(withdrawAmount, "Test");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Amount must be positive");
    }

    [Test]
    public void Freeze_ActiveAccount_ShouldChangeStatusAndPublishEvent()
    {
        // Arrange
        var reason = "Suspicious activity detected";

        // Act
        var result = _account.Freeze(reason);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _account.Status.Should().Be(AccountStatus.Frozen);
        _account.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AccountFrozenEvent>()
            .Which.Reason.Should().Be(reason);
    }

    [Test]
    public void Freeze_AlreadyFrozenAccount_ShouldFail()
    {
        // Arrange
        _account.Freeze("First freeze");
        _account.ClearDomainEvents();

        // Act
        var result = _account.Freeze("Second freeze");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Account is already frozen");
        _account.DomainEvents.Should().BeEmpty();
    }
}

// ❌ Bad: Insufficient entity tests
[TestFixture]
public class AccountTestsBad
{
    [Test]
    public void Deposit_ShouldWork()
    {
        var account = new Account();
        account.Deposit(new Money(100, Currency.USD), "test");
        Assert.That(account.Balance.Amount, Is.EqualTo(100)); // No proper arrangement or comprehensive assertions
    }
}
```

## Application Layer Testing

### Testing Command Handlers

```csharp
// ✅ Good: Comprehensive command handler tests
[TestFixture]
public class CreateAccountCommandHandlerTests : TestBase
{
    private CreateAccountCommandHandler _handler;
    private Mock<IAccountRepository> _mockRepository;
    private Mock<IEventPublisher> _mockEventPublisher;
    private Mock<IMapper> _mockMapper;
    private Mock<ILogger<CreateAccountCommandHandler>> _mockLogger;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        _mockRepository = new Mock<IAccountRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = CreateMockLogger<CreateAccountCommandHandler>();

        _handler = new CreateAccountCommandHandler(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task Handle_ValidCommand_ShouldCreateAccountSuccessfully()
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: 1000m,
            Currency: "USD");

        var expectedAccount = Account.CreateNew(
            "1234567890",
            command.CustomerId,
            new Money(command.InitialDeposit, Currency.USD));

        var expectedDto = new AccountDto
        {
            Id = expectedAccount.Id,
            AccountNumber = expectedAccount.AccountNumber,
            Balance = expectedAccount.Balance.Amount,
            Currency = expectedAccount.Balance.Currency.Code
        };

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback<Account, CancellationToken>((account, _) =>
            {
                // Simulate setting the account number after save
                account.SetAccountNumber("1234567890");
            });

        _mockMapper.Setup(m => m.Map<AccountDto>(It.IsAny<Account>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);

        _mockRepository.Verify(r => r.AddAsync(
            It.Is<Account>(a =>
                a.CustomerId == command.CustomerId &&
                a.Balance.Amount == command.InitialDeposit),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockEventPublisher.Verify(p => p.PublishAsync(
            It.IsAny<AccountCreatedEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Account created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_RepositoryThrowsException_ShouldReturnFailureResult()
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: 1000m,
            Currency: "USD");

        var expectedException = new InvalidOperationException("Database error");
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("error occurred");

        _mockEventPublisher.Verify(p => p.PublishAsync(
            It.IsAny<IDomainEvent>(),
            It.IsAny<CancellationToken>()), Times.Never);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error creating account")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_CancellationRequested_ShouldRespectCancellation()
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: 1000m,
            Currency: "USD");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await FluentActions.Invoking(() => _handler.Handle(command, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}

// ❌ Bad: Insufficient command handler tests
[TestFixture]
public class CreateAccountCommandHandlerTestsBad
{
    [Test]
    public async Task Handle_ShouldCreateAccount()
    {
        var handler = new CreateAccountCommandHandler(null, null, null, null); // Nulls everywhere
        var command = new CreateAccountCommand(Guid.NewGuid(), AccountType.Checking, 1000, "USD");

        // This will throw NullReferenceException
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True); // Will never reach this
    }
}
```

### Testing Query Handlers

```csharp
// ✅ Good: Comprehensive query handler tests
[TestFixture]
public class GetAccountByIdQueryHandlerTests : TestBase
{
    private GetAccountByIdQueryHandler _handler;
    private Mock<IAccountRepository> _mockRepository;
    private Mock<IMapper> _mockMapper;
    private Mock<IMemoryCache> _mockCache;
    private Mock<ILogger<GetAccountByIdQueryHandler>> _mockLogger;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        _mockRepository = new Mock<IAccountRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = CreateMockLogger<GetAccountByIdQueryHandler>();

        _handler = new GetAccountByIdQueryHandler(
            _mockRepository.Object,
            _mockMapper.Object,
            _mockCache.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task Handle_ExistingAccountId_ShouldReturnAccountDto()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountByIdQuery(accountId);

        var account = Account.CreateNew("1234567890", Guid.NewGuid(), new Money(1000m, Currency.USD));
        var expectedDto = new AccountDto
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            Balance = account.Balance.Amount
        };

        _mockRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _mockMapper.Setup(m => m.Map<AccountDto>(account))
            .Returns(expectedDto);

        // Mock cache miss
        object? cachedValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        _mockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedDto);

        _mockRepository.Verify(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<AccountDto>(account), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistentAccountId_ShouldReturnFailure()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountByIdQuery(accountId);

        _mockRepository.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Mock cache miss
        object? cachedValue = null;
        _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Account not found");

        _mockRepository.Verify(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(m => m.Map<AccountDto>(It.IsAny<Account>()), Times.Never);
    }

    [Test]
    public async Task Handle_CachedAccount_ShouldReturnFromCacheWithoutRepositoryCall()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var query = new GetAccountByIdQuery(accountId);

        var cachedDto = new AccountDto
        {
            Id = accountId,
            AccountNumber = "1234567890",
            Balance = 1000m
        };

        // Mock cache hit
        object? cachedValue = cachedDto;
        _mockCache.Setup(c => c.TryGetValue($"account:{accountId}", out cachedValue))
            .Returns(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(cachedDto);

        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockMapper.Verify(m => m.Map<AccountDto>(It.IsAny<Account>()), Times.Never);
    }
}
```

### Testing Validators

```csharp
// ✅ Good: Comprehensive validator tests
[TestFixture]
public class CreateAccountCommandValidatorTests : TestBase
{
    private CreateAccountCommandValidator _validator;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        _validator = new CreateAccountCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_ShouldPassValidation()
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: 1000m,
            Currency: "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_EmptyCustomerId_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.Empty,
            AccountType: AccountType.Checking,
            InitialDeposit: 1000m,
            Currency: "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateAccountCommand.CustomerId) &&
            e.ErrorMessage.Contains("Customer ID is required"));
    }

    [TestCase(0)]
    [TestCase(-100)]
    [TestCase(-0.01)]
    public void Validate_InvalidInitialDeposit_ShouldFailValidation(decimal initialDeposit)
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: initialDeposit,
            Currency: "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateAccountCommand.InitialDeposit) &&
            e.ErrorMessage.Contains("must be positive"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    [TestCase("INVALID")]
    [TestCase("us")]
    [TestCase("USDD")]
    public void Validate_InvalidCurrency_ShouldFailValidation(string? currency)
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: 1000m,
            Currency: currency!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(CreateAccountCommand.Currency));
    }

    [Test]
    public void Validate_ExcessiveInitialDeposit_ShouldFailValidation()
    {
        // Arrange
        var command = new CreateAccountCommand(
            CustomerId: Guid.NewGuid(),
            AccountType: AccountType.Checking,
            InitialDeposit: 1_000_001m, // Over the limit
            Currency: "USD");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(CreateAccountCommand.InitialDeposit) &&
            e.ErrorMessage.Contains("exceed"));
    }
}
```

## API Controller Testing

### Testing Controller Actions

```csharp
// ✅ Good: Comprehensive controller tests
[TestFixture]
public class AccountControllerTests : TestBase
{
    private AccountController _controller;
    private Mock<IMediator> _mockMediator;
    private Mock<ILogger<AccountController>> _mockLogger;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();

        _mockMediator = new Mock<IMediator>();
        _mockLogger = CreateMockLogger<AccountController>();

        _controller = new AccountController(_mockMediator.Object, _mockLogger.Object);

        // Setup controller context for proper action result creation
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Test]
    public async Task GetAccount_ExistingAccount_ShouldReturnOkWithAccountDto()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var expectedDto = new AccountDto
        {
            Id = accountId,
            AccountNumber = "1234567890",
            Balance = 1000m,
            Currency = "USD"
        };

        _mockMediator.Setup(m => m.Send(
            It.Is<GetAccountByIdQuery>(q => q.AccountId == accountId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AccountDto>.Success(expectedDto));

        // Act
        var actionResult = await _controller.GetAccount(accountId);

        // Assert
        var okResult = actionResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<AccountDto>().Subject;
        returnedDto.Should().BeEquivalentTo(expectedDto);

        _mockMediator.Verify(m => m.Send(
            It.Is<GetAccountByIdQuery>(q => q.AccountId == accountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetAccount_NonExistentAccount_ShouldReturnNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();

        _mockMediator.Setup(m => m.Send(
            It.Is<GetAccountByIdQuery>(q => q.AccountId == accountId),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AccountDto>.Failure("Account not found"));

        // Act
        var actionResult = await _controller.GetAccount(accountId);

        // Assert
        actionResult.Result.Should().BeOfType<NotFoundResult>();
    }

    [Test]
    public async Task CreateAccount_ValidRequest_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            CustomerId = Guid.NewGuid(),
            AccountType = AccountType.Checking,
            InitialDeposit = 1000m,
            Currency = "USD"
        };

        var createdAccount = new AccountDto
        {
            Id = Guid.NewGuid(),
            AccountNumber = "1234567890",
            Balance = request.InitialDeposit,
            Currency = request.Currency
        };

        _mockMediator.Setup(m => m.Send(
            It.Is<CreateAccountCommand>(c =>
                c.CustomerId == request.CustomerId &&
                c.InitialDeposit == request.InitialDeposit),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AccountDto>.Success(createdAccount));

        // Act
        var actionResult = await _controller.CreateAccount(request);

        // Assert
        var createdResult = actionResult.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AccountController.GetAccount));
        createdResult.RouteValues.Should().ContainKey("accountId")
            .WhoseValue.Should().Be(createdAccount.Id);

        var returnedDto = createdResult.Value.Should().BeOfType<AccountDto>().Subject;
        returnedDto.Should().BeEquivalentTo(createdAccount);
    }

    [Test]
    public async Task CreateAccount_InvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            CustomerId = Guid.NewGuid(),
            AccountType = AccountType.Checking,
            InitialDeposit = 1000m,
            Currency = "USD"
        };

        _mockMediator.Setup(m => m.Send(
            It.IsAny<CreateAccountCommand>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AccountDto>.Failure("Invalid currency code"));

        // Act
        var actionResult = await _controller.CreateAccount(request);

        // Assert
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Invalid currency code");
    }

    [Test]
    public async Task CreateAccount_ModelValidationFails_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            CustomerId = Guid.Empty, // Invalid
            AccountType = AccountType.Checking,
            InitialDeposit = -100m, // Invalid
            Currency = "INVALID" // Invalid
        };

        _controller.ModelState.AddModelError("CustomerId", "Customer ID is required");
        _controller.ModelState.AddModelError("InitialDeposit", "Amount must be positive");
        _controller.ModelState.AddModelError("Currency", "Invalid currency code");

        // Act
        var actionResult = await _controller.CreateAccount(request);

        // Assert
        var badRequestResult = actionResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var modelState = badRequestResult.Value.Should().BeOfType<SerializableError>().Subject;
        modelState.Should().ContainKeys("CustomerId", "InitialDeposit", "Currency");

        _mockMediator.Verify(m => m.Send(
            It.IsAny<CreateAccountCommand>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

## Test Data Builders

### Using Builder Pattern for Test Data

```csharp
// ✅ Good: Test data builders
public class AccountBuilder
{
    private string _accountNumber = "1234567890";
    private Guid _customerId = Guid.NewGuid();
    private decimal _balance = 1000m;
    private string _currency = "USD";
    private AccountStatus _status = AccountStatus.Active;
    private List<Transaction> _transactions = new();

    public AccountBuilder WithAccountNumber(string accountNumber)
    {
        _accountNumber = accountNumber;
        return this;
    }

    public AccountBuilder WithCustomerId(Guid customerId)
    {
        _customerId = customerId;
        return this;
    }

    public AccountBuilder WithBalance(decimal balance, string currency = "USD")
    {
        _balance = balance;
        _currency = currency;
        return this;
    }

    public AccountBuilder WithStatus(AccountStatus status)
    {
        _status = status;
        return this;
    }

    public AccountBuilder WithTransaction(Transaction transaction)
    {
        _transactions.Add(transaction);
        return this;
    }

    public AccountBuilder WithTransactions(IEnumerable<Transaction> transactions)
    {
        _transactions.AddRange(transactions);
        return this;
    }

    public Account Build()
    {
        var account = Account.CreateNew(_accountNumber, _customerId, new Money(_balance, Currency.FromCode(_currency)));

        // Use reflection to set status if needed
        if (_status != AccountStatus.Active)
        {
            account.Freeze("Test freeze");
            if (_status == AccountStatus.Closed)
            {
                account.Close("Test close");
            }
        }

        // Add transactions
        foreach (var transaction in _transactions)
        {
            account.AddTransaction(transaction);
        }

        account.ClearDomainEvents(); // Clear events created during setup

        return account;
    }

    public static implicit operator Account(AccountBuilder builder) => builder.Build();
}

public class TransactionBuilder
{
    private Guid _accountId = Guid.NewGuid();
    private decimal _amount = 100m;
    private string _currency = "USD";
    private TransactionType _type = TransactionType.Deposit;
    private string _description = "Test transaction";
    private DateTime _timestamp = DateTime.UtcNow;

    public TransactionBuilder WithAccountId(Guid accountId)
    {
        _accountId = accountId;
        return this;
    }

    public TransactionBuilder WithAmount(decimal amount, string currency = "USD")
    {
        _amount = amount;
        _currency = currency;
        return this;
    }

    public TransactionBuilder WithType(TransactionType type)
    {
        _type = type;
        return this;
    }

    public TransactionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public TransactionBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public Transaction Build()
    {
        return _type switch
        {
            TransactionType.Deposit => Transaction.CreateDeposit(_accountId, new Money(_amount, Currency.FromCode(_currency)), _description),
            TransactionType.Withdrawal => Transaction.CreateWithdrawal(_accountId, new Money(_amount, Currency.FromCode(_currency)), _description),
            TransactionType.Transfer => Transaction.CreateTransfer(_accountId, Guid.NewGuid(), new Money(_amount, Currency.FromCode(_currency)), _description),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static implicit operator Transaction(TransactionBuilder builder) => builder.Build();
}

// Usage in tests
[TestFixture]
public class AccountBuilderUsageTests : DomainTestBase
{
    [Test]
    public void AccountBuilder_Usage_Example()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var transaction1 = new TransactionBuilder()
            .WithType(TransactionType.Deposit)
            .WithAmount(500m)
            .WithDescription("Initial deposit");

        var transaction2 = new TransactionBuilder()
            .WithType(TransactionType.Withdrawal)
            .WithAmount(200m)
            .WithDescription("ATM withdrawal");

        var account = new AccountBuilder()
            .WithCustomerId(customerId)
            .WithAccountNumber("9876543210")
            .WithBalance(1500m)
            .WithTransactions(new[] { transaction1, transaction2 })
            .Build();

        // Act & Assert
        account.CustomerId.Should().Be(customerId);
        account.AccountNumber.Should().Be("9876543210");
        account.Balance.Amount.Should().Be(1500m);
        account.Transactions.Should().HaveCount(2);
    }
}
```

## Test Organization and Naming

### Test Method Naming Convention

```csharp
// ✅ Good: Descriptive test names following pattern: MethodName_Scenario_ExpectedBehavior
[TestFixture]
public class AccountTests
{
    [Test]
    public void Withdraw_SufficientFunds_ShouldDecreaseBalanceAndCreateTransaction()
    {
        // Test implementation
    }

    [Test]
    public void Withdraw_InsufficientFunds_ShouldReturnFailureAndNotChangeBalance()
    {
        // Test implementation
    }

    [Test]
    public void Withdraw_FrozenAccount_ShouldReturnFailureWithAppropriateMessage()
    {
        // Test implementation
    }

    [Test]
    public void Withdraw_ZeroAmount_ShouldReturnFailureWithValidationMessage()
    {
        // Test implementation
    }
}

// ❌ Bad: Unclear test names
[TestFixture]
public class AccountTestsBad
{
    [Test]
    public void TestWithdraw() { } // What scenario? What's expected?

    [Test]
    public void Test1() { } // Completely unclear

    [Test]
    public void WithdrawTest() { } // Better but still vague
}
```

### Test Categories and Traits

```csharp
// ✅ Good: Using categories for test organization
[TestFixture]
[Category("Unit")]
[Category("Domain")]
public class AccountTests : DomainTestBase
{
    [Test]
    [Category("BusinessRules")]
    public void Withdraw_ExceedsDailyLimit_ShouldReturnFailure()
    {
        // Test implementation
    }

    [Test]
    [Category("Validation")]
    public void Withdraw_NegativeAmount_ShouldReturnFailure()
    {
        // Test implementation
    }

    [Test]
    [Category("Performance")]
    [Timeout(100)] // Should complete within 100ms
    public void GetBalance_LargeTransactionHistory_ShouldCompleteQuickly()
    {
        // Test implementation
    }
}
```

## Test Utilities and Helpers

### Custom Assertions

```csharp
// ✅ Good: Custom assertion extensions
public static class DomainAssertions
{
    public static void ShouldHaveFailedWith(this Result result, string expectedError)
    {
        result.IsFailure.Should().BeTrue($"Expected result to fail with error: {expectedError}");
        result.Error.Should().Contain(expectedError);
    }

    public static void ShouldHaveSucceeded(this Result result)
    {
        result.IsSuccess.Should().BeTrue($"Expected result to succeed but got error: {result.Error}");
    }

    public static void ShouldHaveDomainEvent<T>(this IEnumerable<IDomainEvent> events) where T : IDomainEvent
    {
        events.Should().ContainSingle(e => e.GetType() == typeof(T),
            $"Expected exactly one event of type {typeof(T).Name}");
    }

    public static void ShouldNotHaveDomainEvents(this IEnumerable<IDomainEvent> events)
    {
        events.Should().BeEmpty("Expected no domain events to be published");
    }
}

// Usage
[Test]
public void Withdraw_InsufficientFunds_ShouldReturnFailure()
{
    // Arrange & Act
    var result = _account.Withdraw(CreateMoney(2000m), "Test withdrawal");

    // Assert
    result.ShouldHaveFailedWith("Insufficient funds");
    _account.DomainEvents.ShouldNotHaveDomainEvents();
}
```

## Performance Testing

### Micro-benchmarks

```csharp
// ✅ Good: Performance testing with BenchmarkDotNet
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
public class MoneyPerformanceBenchmarks
{
    private readonly Money _money1 = new(100m, Currency.USD);
    private readonly Money _money2 = new(50m, Currency.USD);

    [Benchmark]
    public Money AddMoney() => _money1.Add(_money2);

    [Benchmark]
    public Money SubtractMoney() => _money1.Subtract(_money2);

    [Benchmark]
    public bool CompareMoney() => _money1 > _money2;
}

// Memory allocation tests
[TestFixture]
public class MemoryTests
{
    [Test]
    public void CreateManyAccounts_ShouldNotExceedMemoryThreshold()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);
        var accounts = new List<Account>();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            accounts.Add(new AccountBuilder()
                .WithAccountNumber($"ACC{i:D10}")
                .WithBalance(1000m)
                .Build());
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        memoryIncrease.Should().BeLessThan(10 * 1024 * 1024, // 10MB
            "Creating 1000 accounts should not use more than 10MB");
    }
}
```

## Unit Testing Checklist

### Test Coverage Requirements

- [ ] All public methods tested
- [ ] Edge cases and boundary conditions covered
- [ ] Error conditions tested
- [ ] Business rules validated
- [ ] Domain events verified
- [ ] Mocking used appropriately
- [ ] Test data builders utilized
- [ ] Performance characteristics validated

### Test Quality Indicators

- [ ] Tests follow AAA pattern (Arrange, Act, Assert)
- [ ] Test names are descriptive and follow conventions
- [ ] Tests are isolated and independent
- [ ] No test dependencies on external systems
- [ ] Fast execution (< 100ms per test)
- [ ] Deterministic results
- [ ] Single logical assertion per test
- [ ] Meaningful failure messages

## Summary

1. **Write comprehensive tests** - Cover all scenarios including edge cases
2. **Use proper test structure** - Follow AAA pattern consistently
3. **Mock dependencies appropriately** - Isolate units under test
4. **Create meaningful test data** - Use builders and factories
5. **Name tests descriptively** - Make intent clear from name
6. **Test business logic thoroughly** - Focus on domain rules
7. **Verify error conditions** - Test failure scenarios
8. **Use custom assertions** - Make tests more readable
9. **Organize tests logically** - Group related tests together
10. **Monitor test performance** - Keep tests fast and reliable
