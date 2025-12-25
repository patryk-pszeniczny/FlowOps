using System.Diagnostics.Metrics;

namespace FlowOps.BuildingBlocks.Diagnostics
{
    public static class FlowOpsMetrics
    {
        public static readonly Meter Meter = new("FlowOps.Diagnostics", "1.0.0");

        public static readonly Counter<long> DbContextSaveOperations =
            Meter.CreateCounter<long>("flowops.dbcontext.save_changes", "operations", "Number of DbContext SaveChangesAsync calls.");

        public static readonly Counter<long> DbContextSaveFailures =
            Meter.CreateCounter<long>("flowops.dbcontext.save_failures", "operations", "Number of failed DbContext save operations.");

        public static readonly Histogram<double> DbContextSaveDuration =
            Meter.CreateHistogram<double>("flowops.dbcontext.save_duration_ms", unit: "ms", description: "DbContext SaveChangesAsync duration in milliseconds.");

        public static readonly Counter<long> OutboxMessagesProcessed =
            Meter.CreateCounter<long>("flowops.outbox.processed", "messages", "Total number of outbox messages processed.");

        public static readonly Counter<long> OutboxMessagesFailed =
            Meter.CreateCounter<long>("flowops.outbox.failed", "messages", "Total number of outbox messages that failed to publish.");

        public static readonly Counter<long> InboxMessagesProcessed =
            Meter.CreateCounter<long>("flowops.inbox.processed", "messages", "Total number of inbox messages marked as processed.");

        public static readonly Counter<long> DomainEventsHandled =
            Meter.CreateCounter<long>("flowops.domain_events.handled", "events", "Number of domain events handled successfully.");

        public static readonly Counter<long> DomainEventHandlerFailures =
            Meter.CreateCounter<long>("flowops.domain_events.failures", "events", "Number of domain event handler failures.");
    }
}