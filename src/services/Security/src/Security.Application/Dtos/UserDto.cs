namespace Security.Application.Dtos;

/// <summary>
/// User information response model
/// </summary>
public record UserResponse(
    string Id,
    string UserName,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailConfirmed,
    bool IsActive,
    DateTime CreatedAt);