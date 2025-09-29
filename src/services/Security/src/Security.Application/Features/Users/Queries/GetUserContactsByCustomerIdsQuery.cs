using BankSystem.Shared.Domain.Common;
using MediatR;
using Security.Application.Dtos;

namespace Security.Application.Features.Users.Queries;

/// <summary>
/// Query to get multiple user contacts by customer IDs for batch operations
/// </summary>
/// <param name="CustomerIds">The list of customer IDs to search for</param>
public record GetUserContactsByCustomerIdsQuery(IEnumerable<Guid> CustomerIds)
    : IRequest<Result<IEnumerable<UserContactDto>>>;
