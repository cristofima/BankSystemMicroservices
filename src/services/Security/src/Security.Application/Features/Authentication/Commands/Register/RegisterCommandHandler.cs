using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Security.Application.Dtos;
using Security.Application.Interfaces;
using Security.Domain.Common;
using Security.Domain.Entities;

namespace Security.Application.Features.Authentication.Commands.Register;

/// <summary>
/// Handler for user registration command
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<UserResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISecurityAuditService _auditService;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        ISecurityAuditService auditService,
        ILogger<RegisterCommandHandler> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<UserResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        try
        {
            _logger.LogInformation("Processing registration for user {UserName}", request.UserName);

            var validationResult = await ValidateRegistrationRequestAsync(request);
            if (validationResult.IsFailure)
                return Result<UserResponse>.Failure(validationResult.Error);

            var user = CreateUserFromRequest(request);
            var createResult = await CreateUserAsync(user, request.Password);
            
            if (createResult.IsFailure)
                return Result<UserResponse>.Failure(createResult.Error);

            await _auditService.LogUserRegistrationAsync(user.Id, request.IpAddress);

            _logger.LogInformation("User {UserName} registered successfully with ID {UserId}", 
                request.UserName, user.Id);

            var userResponse = CreateUserResponse(user);
            return Result<UserResponse>.Success(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {UserName}", request.UserName);
            return Result<UserResponse>.Failure("An error occurred during registration");
        }
    }

    private async Task<Result> ValidateRegistrationRequestAsync(RegisterCommand request)
    {
        if (!ArePasswordsMatching(request))
            return Result.Failure("Passwords do not match");

        var existingUserValidation = await ValidateUserDoesNotExistAsync(request);
        if (existingUserValidation.IsFailure)
            return existingUserValidation;

        return Result.Success();
    }

    private static bool ArePasswordsMatching(RegisterCommand request)
    {
        return request.Password == request.ConfirmPassword;
    }

    private async Task<Result> ValidateUserDoesNotExistAsync(RegisterCommand request)
    {
        var existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
            return Result.Failure("Username is already taken");

        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
            return Result.Failure("Email is already registered");

        return Result.Success();
    }

    private static ApplicationUser CreateUserFromRequest(RegisterCommand request)
    {
        return new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = false, // Email verification can be implemented later
            FirstName = request.FirstName,
            LastName = request.LastName,
            ClientId = Guid.NewGuid(), // Generate unique client ID
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    private async Task<Result> CreateUserAsync(ApplicationUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User registration failed for {UserName}: {Errors}", user.UserName, errors);
            return Result.Failure($"Registration failed: {errors}");
        }

        return Result.Success();
    }

    private static UserResponse CreateUserResponse(ApplicationUser user)
    {
        return new UserResponse(
            user.Id,
            user.UserName!,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            user.IsActive,
            user.CreatedAt);
    }
}
