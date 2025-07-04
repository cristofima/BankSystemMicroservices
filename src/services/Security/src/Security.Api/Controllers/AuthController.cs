using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Security.Api.Services;
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
[Route("api/v1/auth")]
[Consumes("application/json")]
[Produces("application/json")]
public class AuthController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IMediator mediator, 
        ILogger<AuthController> logger,
        IHttpContextInfoService httpContextInfoService,
        IApiResponseService apiResponseService) 
        : base(httpContextInfoService, apiResponseService)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        var clientIpAddress = HttpContextInfoService.GetClientIpAddress();
        _logger.LogInformation("Login attempt for user {UserName} from IP {IpAddress}", 
            request.UserName, clientIpAddress);

        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var command = CreateLoginCommand(request, clientIpAddress);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return HandleLoginFailure(request.UserName, result.Error);

        var response = CreateTokenResponse(result.Value!);
        LogSuccessfulLogin(request.UserName);
        
        return Success(response);
    }

    private LoginCommand CreateLoginCommand(LoginRequest request, string clientIpAddress)
    {
        return new LoginCommand(
            request.UserName,
            request.Password,
            clientIpAddress,
            HttpContextInfoService.GetDeviceInfo());
    }

    private IActionResult HandleLoginFailure(string userName, string error)
    {
        _logger.LogWarning("Login failed for user {UserName}: {Error}", userName, error);
        return AuthenticationFailed(error);
    }

    private TokenResponse CreateTokenResponse(dynamic tokenData)
    {
        return new TokenResponse(
            tokenData.AccessToken,
            tokenData.RefreshToken,
            tokenData.AccessTokenExpiry,
            tokenData.RefreshTokenExpiry);
    }

    private void LogSuccessfulLogin(string userName)
    {
        _logger.LogInformation("User {UserName} successfully authenticated", userName);
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
        var clientIpAddress = HttpContextInfoService.GetClientIpAddress();
        _logger.LogInformation("Token refresh attempt from IP {IpAddress}", clientIpAddress);

        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var command = CreateRefreshTokenCommand(request, clientIpAddress);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return HandleRefreshTokenFailure(clientIpAddress, result.Error);

        var response = CreateTokenResponse(result.Value!);
        LogSuccessfulTokenRefresh(clientIpAddress);
        
        return Success(response);
    }

    private RefreshTokenCommand CreateRefreshTokenCommand(RefreshTokenRequest request, string clientIpAddress)
    {
        return new RefreshTokenCommand(
            request.AccessToken,
            request.RefreshToken,
            clientIpAddress,
            HttpContextInfoService.GetDeviceInfo());
    }

    private IActionResult HandleRefreshTokenFailure(string clientIpAddress, string error)
    {
        _logger.LogWarning("Token refresh failed from IP {IpAddress}: {Error}", clientIpAddress, error);
        return AuthenticationFailed(error);
    }

    private void LogSuccessfulTokenRefresh(string clientIpAddress)
    {
        _logger.LogInformation("Token successfully refreshed from IP {IpAddress}", clientIpAddress);
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
        var clientIpAddress = HttpContextInfoService.GetClientIpAddress();
        _logger.LogInformation("Token revocation attempt from IP {IpAddress}", clientIpAddress);

        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var command = CreateRevokeTokenCommand(request, clientIpAddress);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return HandleTokenRevocationFailure(clientIpAddress, result.Error);

        LogSuccessfulTokenRevocation(clientIpAddress);
        return SuccessNoContent();
    }

    private RevokeTokenCommand CreateRevokeTokenCommand(RevokeTokenRequest request, string clientIpAddress)
    {
        return new RevokeTokenCommand(
            request.Token,
            clientIpAddress,
            "Manual revocation");
    }

    private IActionResult HandleTokenRevocationFailure(string clientIpAddress, string error)
    {
        _logger.LogWarning("Token revocation failed from IP {IpAddress}: {Error}", clientIpAddress, error);
        return ValidationError(error);
    }

    private void LogSuccessfulTokenRevocation(string clientIpAddress)
    {
        _logger.LogInformation("Token successfully revoked from IP {IpAddress}", clientIpAddress);
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
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return AuthenticationFailed("User ID not found in token");

        var clientIpAddress = HttpContextInfoService.GetClientIpAddress();
        LogLogoutAttempt(userId, clientIpAddress);

        var command = new LogoutCommand(userId, clientIpAddress);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return HandleLogoutFailure(userId, result.Error);

        LogSuccessfulLogout(userId);
        return SuccessNoContent();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    private void LogLogoutAttempt(string userId, string clientIpAddress)
    {
        _logger.LogInformation("Logout attempt for user {UserId} from IP {IpAddress}", userId, clientIpAddress);
    }

    private IActionResult HandleLogoutFailure(string userId, string error)
    {
        _logger.LogWarning("Logout failed for user {UserId}: {Error}", userId, error);
        return ValidationError(error);
    }

    private void LogSuccessfulLogout(string userId)
    {
        _logger.LogInformation("User {UserId} successfully logged out", userId);
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
        var clientIpAddress = HttpContextInfoService.GetClientIpAddress();
        LogRegistrationAttempt(request.UserName, clientIpAddress);

        if (!ModelState.IsValid)
            return ValidationError("Invalid request data");

        var command = CreateRegisterCommand(request, clientIpAddress);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return HandleRegistrationFailure(request.UserName, result.Error);

        LogSuccessfulRegistration(request.UserName);
        return Created(nameof(Register), new { id = result.Value!.Id }, result.Value);
    }

    private void LogRegistrationAttempt(string userName, string clientIpAddress)
    {
        _logger.LogInformation("Registration attempt for user {UserName} from IP {IpAddress}", userName, clientIpAddress);
    }

    private RegisterCommand CreateRegisterCommand(RegisterRequest request, string clientIpAddress)
    {
        return new RegisterCommand(
            request.UserName,
            request.Email,
            request.Password,
            request.ConfirmPassword,
            request.FirstName,
            request.LastName,
            clientIpAddress,
            HttpContextInfoService.GetDeviceInfo());
    }

    private IActionResult HandleRegistrationFailure(string userName, string error)
    {
        _logger.LogWarning("Registration failed for user {UserName}: {Error}", userName, error);
        
        return error.Contains("already", StringComparison.OrdinalIgnoreCase) 
            ? Conflict(error) 
            : ValidationError(error);
    }

    private void LogSuccessfulRegistration(string userName)
    {
        _logger.LogInformation("User {UserName} successfully registered", userName);
    }
}