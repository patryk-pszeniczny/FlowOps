using FlowOps.Events;
using FlowOps.Reports.Stores;

namespace FlowOps.Services.Reporting
{
    public class ReportingHandler : IReportingHandler
    {
        private readonly IReportingStore _store;
        private readonly ILogger<ReportingHandler> _logger;
        public ReportingHandler(IReportingStore store, ILogger<ReportingHandler> logger)
        {
            _store = store;
            _logger = logger;
        }
        public Task On(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default)
        {
            var snapshot = _store.UpsertOnActivation(ev.CustomerId);
            _logger.LogInformation(
                "Report updated for CustomerId: {CustomerId}, ActiveSubscriptions: {ActiveSubscriptions}",
                ev.CustomerId, snapshot.ActiveSubscriptions);
            return Task.CompletedTask;
        }

        public Task On(InvoiceIssuedEvent ev, CancellationToken cancellationToken = default)
        {
            var report = _store.GetOrAdd(ev.CustomerId);

            report.TotalInvoiced += ev.Amount;

            _logger.LogInformation(
                "Report invoiced updated for CustomerId: {CustomerId}, +{Amount} {Currency}, TotalInvoiced={Total}",
                ev.CustomerId, ev.Amount, ev.Currency, report.TotalInvoiced);
            return Task.CompletedTask;
        }

        public Task On(InvoicePaidEvent ev, CancellationToken cancellationToken = default)
        {
           var report = _store.GetOrAdd(ev.CustomerId);

            report.TotalPaid += ev.Amount;

            _logger.LogInformation(
                "Report paid updated for CustomerId: {CustomerId}, +{Amount} {Currency}, TotalPaid={Total}",
                ev.CustomerId, ev.Amount, ev.Currency, report.TotalPaid);
            return Task.CompletedTask;
        }

        public Task On(SubscriptionCancelledEvent ev, CancellationToken cancellationToken = default)
        {
            var report = _store.GetOrAdd(ev.CustomerId);
            if(report.ActiveSubscriptions > 0)
            {
                report.ActiveSubscriptions -= 1;
            }
            _logger.LogInformation(
                "Report cancelled updated for CustomerId: {CustomerId}, ActiveSubscriptions={Active}",
                ev.CustomerId, report.ActiveSubscriptions
            );
            return Task.CompletedTask;
        }
    }
}
