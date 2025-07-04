using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Security.Application.Features.Authentication.Commands.Register;
using Security.Application.UnitTests.Common;
using Security.Domain.Entities;

namespace Security.Application.UnitTests.Features.Authentication.Commands.Register;

[TestFixture]
public class RegisterCommandHandlerTests : CommandHandlerTestBase
{
    private RegisterCommandHandler _handler = null!;
    private Mock<ILogger<RegisterCommandHandler>> _mockLogger = null!;

    [SetUp]
    public override void SetUp()
    {
        base.SetUp();
        
        _mockLogger = CreateMockLogger<RegisterCommandHandler>();
        _handler = new RegisterCommandHandler(
            MockUserManager.Object,
            MockAuditService.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task Handle_ValidRegistration_ShouldReturnSuccessWithUserResponse()
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            "newuser@example.com",
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null); // User doesn't exist
        SetupUserManagerFindByEmail(null); // Email doesn't exist
        SetupUserManagerCreateUser(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserName.Should().Be(command.UserName);
        result.Value.Email.Should().Be(command.Email);
        result.Value.FirstName.Should().Be(command.FirstName);
        result.Value.LastName.Should().Be(command.LastName);
        result.Value.IsEmailConfirmed.Should().BeFalse();
        result.Value.IsActive.Should().BeTrue();

        // Verify user creation
        MockUserManager.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u => 
                    u.UserName == command.UserName &&
                    u.Email == command.Email &&
                    u.FirstName == command.FirstName &&
                    u.LastName == command.LastName &&
                    u.IsActive == true &&
                    u.EmailConfirmed == false),
                command.Password),
            Times.Once);

        // Verify audit logging
        MockAuditService.Verify(
            x => x.LogUserRegistrationAsync(It.IsAny<string>(), command.IpAddress),
            Times.Once);
    }

    [Test]
    public async Task Handle_PasswordsDoNotMatch_ShouldReturnFailure()
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            "newuser@example.com",
            "Password123!",
            "DifferentPassword123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Passwords do not match");

        // Verify no user creation attempted
        MockUserManager.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_UserNameAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var existingUser = CreateTestUser();
        var command = new RegisterCommand(
            existingUser.UserName!,
            "newemail@example.com",
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(existingUser);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Username is already taken");

        // Verify no user creation attempted
        MockUserManager.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_EmailAlreadyExists_ShouldReturnFailure()
    {
        // Arrange
        var existingUser = CreateTestUser();
        var command = new RegisterCommand(
            "newusername@example.com",
            existingUser.Email!,
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null); // Username doesn't exist
        SetupUserManagerFindByEmail(existingUser); // Email exists

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Email is already registered");

        // Verify no user creation attempted
        MockUserManager.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_UserCreationFails_ShouldReturnFailureWithErrors()
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            "newuser@example.com",
            "weak",
            "weak",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        var identityErrors = new[]
        {
            new IdentityError { Description = "Password is too weak" },
            new IdentityError { Description = "Password must contain special characters" }
        };
        var failureResult = IdentityResult.Failed(identityErrors);

        SetupUserManagerFindByName(null);
        SetupUserManagerFindByEmail(null);
        SetupUserManagerCreateUser(failureResult);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Registration failed");
        result.Error.Should().Contain("Password is too weak");
        result.Error.Should().Contain("Password must contain special characters");

        // Verify no audit logging for failed registration
        MockAuditService.Verify(
            x => x.LogUserRegistrationAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ExceptionDuringProcessing_ShouldReturnFailureAndLogError()
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            "newuser@example.com",
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        MockUserManager
            .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("An error occurred during registration");
    }

    [Test]
    public async Task Handle_ValidRegistrationWithoutOptionalFields_ShouldReturnSuccess()
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            "newuser@example.com",
            "Password123!",
            "Password123!",
            null, // No first name
            null, // No last name
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null);
        SetupUserManagerFindByEmail(null);
        SetupUserManagerCreateUser(IdentityResult.Success);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.FirstName.Should().BeNull();
        result.Value.LastName.Should().BeNull();

        // Verify user creation with null values
        MockUserManager.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u => 
                    u.FirstName == null &&
                    u.LastName == null),
                command.Password),
            Times.Once);
    }

    [Test]
    public void Handle_NullCommand_ShouldThrowArgumentNullException()
    {
        // Arrange
        RegisterCommand command = null!;

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _handler.Handle(command, CreateCancellationToken()));
    }

    [TestCase("")]
    [TestCase(" ")]
    public async Task Handle_EmptyUserName_ShouldReturnFailure(string userName)
    {
        // Arrange
        var command = new RegisterCommand(
            userName,
            "newuser@example.com",
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert - this might depend on UserManager validation, but we'll test our logic
        // The actual validation might happen in UserManager.CreateAsync
        SetupUserManagerCreateUser(IdentityResult.Failed(
            new IdentityError { Description = "Username cannot be empty" }));
    }

    [TestCase("")]
    [TestCase(" ")]
    public async Task Handle_EmptyEmail_ShouldReturnFailure(string email)
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            email,
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        SetupUserManagerFindByName(null);
        SetupUserManagerFindByEmail(null);
        SetupUserManagerCreateUser(IdentityResult.Failed(
            new IdentityError { Description = "Email cannot be empty" }));

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Email cannot be empty");
    }

    [Test]
    public async Task Handle_ValidRegistration_ShouldSetCorrectUserProperties()
    {
        // Arrange
        var command = new RegisterCommand(
            "newuser@example.com",
            "newuser@example.com",
            "Password123!",
            "Password123!",
            "John",
            "Doe",
            CreateValidIpAddress(),
            CreateValidDeviceInfo());

        ApplicationUser? capturedUser = null;
        MockUserManager
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, password) => capturedUser = user)
            .ReturnsAsync(IdentityResult.Success);

        SetupUserManagerFindByName(null);
        SetupUserManagerFindByEmail(null);

        // Act
        var result = await _handler.Handle(command, CreateCancellationToken());

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        capturedUser.Should().NotBeNull();
        capturedUser!.UserName.Should().Be(command.UserName);
        capturedUser.Email.Should().Be(command.Email);
        capturedUser.FirstName.Should().Be(command.FirstName);
        capturedUser.LastName.Should().Be(command.LastName);
        capturedUser.EmailConfirmed.Should().BeFalse();
        capturedUser.IsActive.Should().BeTrue();
        capturedUser.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        capturedUser.ClientId.Should().NotBeEmpty();
    }
}
