using BankSystem.Shared.Domain.Common;
using MediatR;
using Security.Application.Dtos;

namespace Security.Application.Features.Users.Queries;

/// <summary>
/// Query to get user contact information by customer ID for inter-service communication
/// </summary>
/// <param name="CustomerId">The customer ID to search for</param>
public record GetUserContactByCustomerIdQuery(Guid CustomerId) : IRequest<Result<UserContactDto>>;
