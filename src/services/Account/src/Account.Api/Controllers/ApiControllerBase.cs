using BankSystem.Shared.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace BankSystem.Account.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleFailure(Result result, string? title = null)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(CreateProblemDetails(title ?? "Resource Not Found", result.Error, StatusCodes.Status404NotFound)),
            ErrorType.Validation => BadRequest(CreateProblemDetails(title ?? "Validation Error", result.Error, StatusCodes.Status400BadRequest)),
            ErrorType.Conflict => Conflict(CreateProblemDetails(title ?? "Conflict", result.Error, StatusCodes.Status409Conflict)),
            _ => BadRequest(CreateProblemDetails(title ?? "Bad Request", result.Error, StatusCodes.Status400BadRequest))
        };
    }

    private static ProblemDetails CreateProblemDetails(string title, string detail, int statusCode)
    {
        return new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{statusCode}",
            Title = title,
            Detail = detail,
            Status = statusCode
        };
    }
}