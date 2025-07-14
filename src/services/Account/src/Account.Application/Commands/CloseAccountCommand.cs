using MediatR;
using BankSystem.Shared.Domain.Common;

namespace BankSystem.Account.Application.Commands;

public record CloseAccountCommand(
    string AccountNumber,
    string Reason) : IRequest<Result>;
