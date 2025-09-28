using BankSystem.Shared.Domain.Validation;
using Microsoft.AspNetCore.Mvc;
using Security.Api.Services;

namespace Security.Api.Controllers;

/// <summary>
/// Base controller with common functionality following Clean Code principles
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected readonly HttpContextInfoService HttpContextInfoService;
    private readonly ApiResponseService _apiResponseService;

    protected BaseController(
        HttpContextInfoService httpContextInfoService,
        ApiResponseService apiResponseService
    )
    {
        Guard.AgainstNull(httpContextInfoService);
        Guard.AgainstNull(apiResponseService);

        HttpContextInfoService = httpContextInfoService;
        _apiResponseService = apiResponseService;
    }

    /// <summary>
    /// Creates a success response with data
    /// </summary>
    protected IActionResult Success<T>(T data)
    {
        return Ok(data);
    }

    /// <summary>
    /// Creates a created response with location
    /// </summary>
    protected IActionResult Created<T>(string actionName, object routeValues, T data)
    {
        return CreatedAtAction(actionName, routeValues, data);
    }

    /// <summary>
    /// Creates a bad request response with validation errors
    /// </summary>
    protected IActionResult ValidationError(string message)
    {
        var problemDetails = _apiResponseService.CreateValidationFailedResponse(
            message,
            Request.Path
        );
        return BadRequest(problemDetails);
    }

    /// <summary>
    /// Creates an unauthorized response
    /// </summary>
    protected IActionResult AuthenticationFailed(string message)
    {
        var problemDetails = _apiResponseService.CreateAuthenticationFailedResponse(
            message,
            Request.Path
        );
        return Unauthorized(problemDetails);
    }

    /// <summary>
    /// Creates a not found response
    /// </summary>
    protected IActionResult ResourceNotFound(string message)
    {
        var problemDetails = _apiResponseService.CreateNotFoundResponse(message, Request.Path);
        return NotFound(problemDetails);
    }

    /// <summary>
    /// Creates a conflict response
    /// </summary>
    protected IActionResult Conflict(string message)
    {
        var problemDetails = _apiResponseService.CreateProblemDetails(
            "Conflict",
            message,
            409,
            Request.Path
        );
        return Conflict(problemDetails);
    }

    /// <summary>
    /// Creates an unprocessable entity response
    /// </summary>
    protected IActionResult UnprocessableEntity(string message)
    {
        var problemDetails = _apiResponseService.CreateProblemDetails(
            "Unprocessable Entity",
            message,
            422,
            Request.Path
        );
        return UnprocessableEntity(problemDetails);
    }

    /// <summary>
    /// Creates a no content response for successful operations without return data
    /// </summary>
    protected IActionResult SuccessNoContent()
    {
        return NoContent();
    }
}
