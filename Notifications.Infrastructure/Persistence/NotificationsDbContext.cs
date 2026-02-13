using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Notifications.Entities;
using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Persistence
{
    public class NotificationsDbContext : DbContext, INotificationsDbContext
    {
        public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : base(options) { }

        public DbSet<TransferNotification> Notifications => Set<TransferNotification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        }

        // Puerto
        public Task<bool> ExistsByTransferIdAsync(Guid transferId, CancellationToken ct = default)
            => Notifications.AnyAsync(n => n.TransferId == transferId, ct);

        public Task AddAsync(TransferNotification notification, CancellationToken ct = default)
        {
            Notifications.Add(notification); return Task.CompletedTask;
        }

        public override Task<int> SaveChangesAsync(CancellationToken ct = default)
            => base.SaveChangesAsync(ct);
    }
}
