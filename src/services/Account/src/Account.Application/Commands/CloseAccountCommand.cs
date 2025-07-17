using MediatR;
using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Application.Commands;

public record CloseAccountCommand(
    Guid AccountId,
    string Reason) : IRequest<Result>;
