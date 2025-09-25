using System.Diagnostics.CodeAnalysis;
using BankSystem.Shared.Application.Interfaces;
using BankSystem.Shared.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace BankSystem.Shared.Application.Behaviours;

/// <summary>
/// Pipeline behavior for handling validation
/// </summary>
/// <typeparam name="TRequest">The request type</typeparam>
/// <typeparam name="TResponse">The response type</typeparam>
[ExcludeFromCodeCoverage]
public sealed class ValidationPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!_validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationFailures = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        var errorsDictionary = validationFailures
            .Where(validationResult => !validationResult.IsValid)
            .SelectMany(validationResult => validationResult.Errors)
            .Where(x => x != null)
            .GroupBy(
                x => x.PropertyName,
                x => x.ErrorMessage,
                (propertyName, errorMessages) =>
                    new { Key = propertyName, Values = errorMessages.Distinct().ToArray() }
            )
            .ToDictionary(x => x.Key, x => x.Values);

        if (errorsDictionary.Count == 0)
            return await next(cancellationToken);

        var title = request is IValidationRequest validationRequest
            ? validationRequest.ValidationErrorTitle()
            : "Validation Error";

        throw new CustomValidationException(errorsDictionary!, title);
    }
}
