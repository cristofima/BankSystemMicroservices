using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Application.Dtos;
using Security.Application.Features.Authentication.Commands.Login;
using Security.Application.Features.Authentication.Commands.Logout;
using Security.Application.Features.Authentication.Commands.RefreshToken;
using Security.Application.Features.Authentication.Commands.Register;
using Security.Application.Features.Authentication.Commands.RevokeToken;
using System.Net;

namespace Security.Api.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and return access/refresh tokens
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authentication tokens</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="429">Too many requests</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for user {UserName} from IP {IpAddress}", 
            request.UserName, GetClientIpAddress());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new LoginCommand(
            request.UserName,
            request.Password,
            GetClientIpAddress(),
            GetDeviceInfo());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Login failed for user {UserName}: {Error}", request.UserName, result.Error);
            return Unauthorized(CreateProblemDetails("Authentication failed", result.Error, 401));
        }

        var response = new TokenResponse(
            result.Value!.AccessToken,
            result.Value.RefreshToken,
            result.Value.AccessTokenExpiry,
            result.Value.RefreshTokenExpiry);

        _logger.LogInformation("User {UserName} successfully authenticated", request.UserName);
        return Ok(response);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Token refresh request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New authentication tokens</returns>
    /// <response code="200">Token refresh successful</response>
    /// <response code="400">Invalid request data or tokens</response>
    /// <response code="401">Invalid refresh token</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token refresh attempt from IP {IpAddress}", GetClientIpAddress());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new RefreshTokenCommand(
            request.AccessToken,
            request.RefreshToken,
            GetClientIpAddress(),
            GetDeviceInfo());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Token refresh failed from IP {IpAddress}: {Error}", 
                GetClientIpAddress(), result.Error);
            return Unauthorized(CreateProblemDetails("Token refresh failed", result.Error, 401));
        }

        var response = new TokenResponse(
            result.Value!.AccessToken,
            result.Value.RefreshToken,
            result.Value.AccessTokenExpiry,
            result.Value.RefreshTokenExpiry);

        _logger.LogInformation("Token successfully refreshed from IP {IpAddress}", GetClientIpAddress());
        return Ok(response);
    }

    /// <summary>
    /// Revoke a refresh token
    /// </summary>
    /// <param name="request">Token revocation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Revocation result</returns>
    /// <response code="204">Token revoked successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="404">Token not found</response>
    [HttpPost("revoke")]
    [Authorize]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token revocation attempt from IP {IpAddress}", GetClientIpAddress());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new RevokeTokenCommand(
            request.Token,
            GetClientIpAddress(),
            "Manual revocation");

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Token revocation failed from IP {IpAddress}: {Error}", 
                GetClientIpAddress(), result.Error);
            return BadRequest(CreateProblemDetails("Token revocation failed", result.Error, 400));
        }

        _logger.LogInformation("Token successfully revoked from IP {IpAddress}", GetClientIpAddress());
        return NoContent();
    }

    /// <summary>
    /// Logout user by revoking all refresh tokens
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Logout result</returns>
    /// <response code="204">Logout successful</response>
    /// <response code="401">Unauthorized</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(CreateProblemDetails("Invalid user context", "User ID not found in token", 401));
        }

        _logger.LogInformation("Logout attempt for user {UserId} from IP {IpAddress}", 
            userId, GetClientIpAddress());

        var command = new LogoutCommand(userId, GetClientIpAddress());
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Logout failed for user {UserId}: {Error}", userId, result.Error);
            return BadRequest(CreateProblemDetails("Logout failed", result.Error, 400));
        }
        
        _logger.LogInformation("User {UserId} successfully logged out", userId);
        return NoContent();
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User information</returns>
    /// <response code="201">Registration successful</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">User already exists</response>
    /// <response code="429">Too many requests</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration attempt for user {UserName} from IP {IpAddress}", 
            request.UserName, GetClientIpAddress());

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var command = new RegisterCommand(
            request.UserName,
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.FirstName,
            request.LastName,
            GetClientIpAddress(),
            GetDeviceInfo());

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Registration failed for user {UserName}: {Error}", request.UserName, result.Error);
            
            var statusCode = result.Error.Contains("already") ? 409 : 400;
            return StatusCode(statusCode, CreateProblemDetails("Registration failed", result.Error, statusCode));
        }

        _logger.LogInformation("User {UserName} successfully registered", request.UserName);
        return CreatedAtAction(nameof(Register), new { id = result.Value!.Id }, result.Value);
    }

    private string? GetClientIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ??
               Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               Request.Headers["X-Real-IP"].FirstOrDefault();
    }

    private string? GetDeviceInfo()
    {
        return Request.Headers["User-Agent"].FirstOrDefault();
    }

    private ProblemDetails CreateProblemDetails(string title, string detail, int statusCode)
    {
        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = Request.Path
        };
    }
}