using FlowOps;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Infrastructure.Messaging;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace FlowOps.Tests.Infrastructure.Web
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public TestWebApplicationFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<FlowOpsDbContext>));
                services.AddDbContext<FlowOpsDbContext>(options =>
                {
                    options.UseSqlServer(_connection);
                });

                services.RemoveAll<RabbitMqEventBus>();
                services.RemoveAll<IEventBus>();
                services.AddSingleton<IEventBus, InMemoryEventBus>();

                services.RemoveAll<IIntegrationEventStore>();
                services.AddSingleton<IIntegrationEventStore, EfCoreIntegrationEventStore>();

                services.AddLogging();

                var provider = services.BuildServiceProvider();
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
