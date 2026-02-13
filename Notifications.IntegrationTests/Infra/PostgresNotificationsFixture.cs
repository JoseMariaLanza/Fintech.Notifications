using Microsoft.EntityFrameworkCore;
using Notifications.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Notifications.IntegrationTests.Infra
{
    /// <summary>
    /// Fixture = prepara/limpia recursos compartidos para los tests (Postgres efímero).
    /// </summary>
    public sealed class PostgresNotificationsFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("NotificationsDb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public NotificationsDbContext Db { get; private set; } = default!;

        public async Task InitializeAsync()
        {
            await _pg.StartAsync();
            var opts = new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseNpgsql(_pg.GetConnectionString()).Options;
            Db = new NotificationsDbContext(opts);
            await Db.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            await Db.DisposeAsync();
            await _pg.DisposeAsync();
        }
    }
}
