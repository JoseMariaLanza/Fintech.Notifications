using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notifications.Domain.Notifications.Entities;

namespace Notifications.Infrastructure.Persistence.Configurations
{
    public class TransferNotificationConfiguration : IEntityTypeConfiguration<TransferNotification>
    {
        public void Configure(EntityTypeBuilder<TransferNotification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Version).IsRequired().HasDefaultValue((short)1);

            builder.Property(x => x.TransferId).IsRequired();
            builder.Property(x => x.FromAccountId).IsRequired();
            builder.Property(x => x.ToAccountId).IsRequired();
            builder.Property(x => x.Amount).IsRequired().HasColumnType("numeric(18,2)");
            builder.Property(x => x.OccurredAtUtc).IsRequired();

            // JSONB (Npgsql)
            builder.Property(x => x.RawPayload).HasColumnType("jsonb");

            // Índices
            builder.HasIndex(x => x.TransferId).HasDatabaseName("ix_notifications_transferid");
            builder.HasIndex(x => new { x.Type, x.Version, x.OccurredAtUtc })
                .HasDatabaseName("ix_notifications_type_version_when");
        }
    }
}
