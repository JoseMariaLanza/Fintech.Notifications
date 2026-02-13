using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Domain.Notifications.Entities;
using System.Text.Json;

namespace Notifications.Application.Transfers.Commands.LogTransfer;

public class LogTransferCommandHandler : IRequestHandler<LogTransferCommand>
{
    private readonly INotificationsDbContext _dbContext;

    public LogTransferCommandHandler(INotificationsDbContext db) => _dbContext = db;

    public async Task Handle(LogTransferCommand command, CancellationToken cancellationToken)
    {
        // TransferId Idempotencia
        if (await _dbContext.ExistsByTransferIdAsync(command.TransferId, cancellationToken)) return;

        // Serializamos la “vista” del evento como JSONB (auditoría)
        using var raw = JsonSerializer.SerializeToDocument(new
        {
            command.Type,
            command.Version,
            command.TransferId,
            command.FromAccountId,
            command.ToAccountId,
            command.Amount,
            command.OccurredAtUtc
        });

        // Creamos la entidad de cominio (NO EF)
        var entity = TransferNotification.FromEvent(
            type: command.Type,
            version: command.Version,
            transferId: command.TransferId,
            from: command.FromAccountId,
            to: command.ToAccountId,
            amount: command.Amount,
            occurredAtUtc: command.OccurredAtUtc,
            raw: raw
        );

        await _dbContext.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}