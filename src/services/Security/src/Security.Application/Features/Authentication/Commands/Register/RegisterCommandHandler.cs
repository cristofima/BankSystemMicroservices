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
        _userManager = userManager;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<Result<UserResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing registration for user {UserName}", request.UserName);

            // Check if passwords match
            if (request.Password != request.ConfirmPassword)
            {
                return Result<UserResponse>.Failure("Passwords do not match");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
            {
                return Result<UserResponse>.Failure("Username is already taken");
            }

            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                return Result<UserResponse>.Failure("Email is already registered");
            }

            // Create new user
            var user = new ApplicationUser
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

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed for {UserName}: {Errors}", request.UserName, errors);
                return Result<UserResponse>.Failure($"Registration failed: {errors}");
            }

            // Audit log
            await _auditService.LogUserRegistrationAsync(user.Id, request.IpAddress);

            _logger.LogInformation("User {UserName} registered successfully with ID {UserId}", 
                request.UserName, user.Id);

            var userResponse = new UserResponse(
                user.Id,
                user.UserName!,
                user.Email!,
                user.FirstName,
                user.LastName,
                user.EmailConfirmed,
                user.IsActive,
                user.CreatedAt);

            return Result<UserResponse>.Success(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {UserName}", request.UserName);
            return Result<UserResponse>.Failure("An error occurred during registration");
        }
    }
}
