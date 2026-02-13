using Notifications.Domain.Notifications.Entities;

namespace Notifications.Application.Abstractions
{
    public interface INotificationsDbContext
    {
        Task<bool> ExistsByTransferIdAsync(Guid transferId, CancellationToken ct = default);
        Task AddAsync(TransferNotification notification, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
