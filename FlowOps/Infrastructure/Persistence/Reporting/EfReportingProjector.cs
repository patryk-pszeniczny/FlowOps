using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence.Reporting
{
    public sealed class EfReportingProjector : IHostedService
    {
        private readonly IEventBus _bus;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EfReportingProjector> _logger;

        public EfReportingProjector(
            IEventBus bus,
            IServiceScopeFactory scopeFactory,
            ILogger<EfReportingProjector> logger)
        {
            _bus = bus;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _bus.Subscribe<SubscriptionActivatedEvent>(OnActivated);
            _bus.Subscribe<SubscriptionCancelledEvent>(OnCancelled);
            _bus.Subscribe<SubscriptionSuspendedEvent>(OnSuspended);
            _bus.Subscribe<SubscriptionResumedEvent>(OnResumed);
            _bus.Subscribe<InvoiceIssuedEvent>(OnInvoiced);
            _bus.Subscribe<InvoicePaidEvent>(OnPaid);

            _logger.LogInformation("EfReportingProjector subscribed to reporting events.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private Task OnActivated(SubscriptionActivatedEvent ev) =>
            WithDbContext(ev.CustomerId, async (db, report, ct) =>
            {
                var exists = await db.ActiveSubscriptions.AnyAsync(
                    x => x.CustomerId == ev.CustomerId && x.SubscriptionId == ev.SubscriptionId,
                    ct);

                if (!exists)
                {
                    db.ActiveSubscriptions.Add(new ActiveSubscription
                    {
                        CustomerId = ev.CustomerId,
                        SubscriptionId = ev.SubscriptionId
                    });
                    report.ActiveSubscriptions += 1;
                }
            });

        private Task OnCancelled(SubscriptionCancelledEvent ev) =>
            RemoveActive(ev.CustomerId, ev.SubscriptionId);

        private Task OnSuspended(SubscriptionSuspendedEvent ev) =>
            RemoveActive(ev.CustomerId, ev.SubscriptionId);

        private Task OnResumed(SubscriptionResumedEvent ev) =>
            WithDbContext(ev.CustomerId, async (db, report, ct) =>
            {
                var exists = await db.ActiveSubscriptions.AnyAsync(
                    x => x.CustomerId == ev.CustomerId && x.SubscriptionId == ev.SubscriptionId,
                    ct);

                if (!exists)
                {
                    db.ActiveSubscriptions.Add(new ActiveSubscription
                    {
                        CustomerId = ev.CustomerId,
                        SubscriptionId = ev.SubscriptionId
                    });
                    report.ActiveSubscriptions += 1;
                }
            });

        private Task OnInvoiced(InvoiceIssuedEvent ev) =>
            WithDbContext(ev.CustomerId, async (_, report, ct) =>
            {
                report.TotalInvoiced += ev.Amount;
            });

        private Task OnPaid(InvoicePaidEvent ev) =>
            WithDbContext(ev.CustomerId, async (_, report, ct) =>
            {
                report.TotalPaid += ev.Amount;
            });

        private Task RemoveActive(Guid customerId, Guid subscriptionId) =>
            WithDbContext(customerId, async (db, report, ct) =>
            {
                var entry = await db.ActiveSubscriptions
                    .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.SubscriptionId == subscriptionId, ct);

                if (entry is not null)
                {
                    db.ActiveSubscriptions.Remove(entry);
                    report.ActiveSubscriptions = Math.Max(0, report.ActiveSubscriptions - 1);
                }
            });

        private async Task WithDbContext(Guid customerId, Func<FlowOpsDbContext, CustomerReport, CancellationToken, Task> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();

            var ct = CancellationToken.None;
            var report = await dbContext.CustomerReports.FirstOrDefaultAsync(r => r.CustomerId == customerId, ct);
            if (report is null)
            {
                report = new CustomerReport
                {
                    CustomerId = customerId,
                    ActiveSubscriptions = 0,
                    TotalInvoiced = 0,
                    TotalPaid = 0
                };
                dbContext.CustomerReports.Add(report);
            }

            await action(dbContext, report, ct);

            await dbContext.SaveChangesAsync(ct);
        }
    }
}