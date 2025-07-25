using BankSystem.Account.Application.Interfaces;
using BankSystem.Shared.Domain.Common;
using MediatR;

namespace BankSystem.Account.Application.Commands;

public interface IAccountActionCommand : IRequest<Result>, IValidationRequest
{
    Guid AccountId { get; }
}