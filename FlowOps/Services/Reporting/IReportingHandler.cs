using FlowOps.Events;

namespace FlowOps.Services.Reporting
{
    public interface IReportingHandler
    {
        Task On(SubscriptionActivatedEvent ev, CancellationToken cancellationToken = default);
        Task On(InvoiceIssuedEvent ev, CancellationToken cancellationToken = default);
        Task On(InvoicePaidEvent ev, CancellationToken cancellationToken = default);
        Task On(SubscriptionCancelledEvent ev, CancellationToken cancellation = default);
        Task On(SubscriptionSuspendedEvent ev, CancellationToken cancellation = default);
        Task On(SubscriptionResumedEvent ev, CancellationToken cancellation = default);
    }
}
