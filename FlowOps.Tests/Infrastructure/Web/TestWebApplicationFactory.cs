using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Infrastructure.Messaging;
using FlowOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FlowOps.Tests.Infrastructure.Web
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        private readonly SqliteConnection _connection;

        public TestWebApplicationFactory()
        {

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<FlowOpsDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.RemoveAll<IConfigureOptions<DbContextOptions<FlowOpsDbContext>>>();
                services.RemoveAll<IConfigureOptions<DbContextOptions>>();
                services.RemoveAll<IConfigureNamedOptions<DbContextOptions<FlowOpsDbContext>>>();
                services.RemoveAll<IConfigureNamedOptions<DbContextOptions>>();

                services.AddDbContext<FlowOpsDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                    options.EnableDetailedErrors();
        
                });

                services.RemoveAll<RabbitMqEventBus>();
                services.RemoveAll<IEventBus>();
                services.AddSingleton<IEventBus, InMemoryEventBus>();

                services.RemoveAll<IIntegrationEventStore>();
                services.AddSingleton<IIntegrationEventStore, EfCoreIntegrationEventStore>();

                services.AddLogging();

     
                using var provider = services.BuildServiceProvider();
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}
