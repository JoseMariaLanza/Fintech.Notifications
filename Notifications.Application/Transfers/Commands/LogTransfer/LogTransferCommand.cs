using MediatR;

namespace Notifications.Application.Transfers.Commands.LogTransfer;

public sealed record LogTransferCommand(
    string Type,               // "TransferCompleted"
    short Version,             // 1
    Guid TransferId,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal Amount,
    DateTime OccurredAtUtc
) : IRequest;
