using System.Text.Json;

namespace Notifications.Domain.Notifications.Entities;

public class TransferNotification
{
    public Guid Id { get; private set; } = Guid.NewGuid();       // PK
    public string Type { get; private set; } = default!;         // "TransferCompleted"
    public short Version { get; private set; }                   // 1 (versionado del contrato)
    public Guid TransferId { get; private set; }                 // búsquedas/idempotencia
    public Guid FromAccountId { get; private set; }
    public Guid ToAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }          // timestamp del evento
    public JsonDocument RawPayload { get; private set; } = default!; // copia JSONB

    private TransferNotification() { }

    private TransferNotification(
        string type,
        short version,
        Guid transferId,
        Guid from,
        Guid to,
        decimal amount,
        DateTime occurredAtUtc,
        JsonDocument raw)
    {
        if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Type = type;
        Version = version;
        TransferId = transferId;
        FromAccountId = from;
        ToAccountId = to;
        Amount = amount;
        OccurredAtUtc = occurredAtUtc;
        RawPayload = raw ?? throw new ArgumentNullException(nameof(raw));
    }

    public static TransferNotification FromEvent(
        string type,
        short version,
        Guid transferId,
        Guid from,
        Guid to,
        decimal amount,
        DateTime occurredAtUtc,
        JsonDocument raw) => new (type, version, transferId, from, to, amount, occurredAtUtc, raw);

    //public TransferNotification(Guid transferId, Guid fromAccountId, Guid toAccountId, decimal amount)
    //{
    //    TransferId = transferId;
    //    FromAccountId = fromAccountId;
    //    ToAccountId = toAccountId;
    //    Amount = amount;
    //}
}