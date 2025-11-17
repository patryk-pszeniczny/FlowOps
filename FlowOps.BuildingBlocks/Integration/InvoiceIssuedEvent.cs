using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.Events
{
    public class InvoiceIssuedEvent : IntegrationEvent
    {
        public Guid InoiceId { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SubscriptionId { get; init; }
        public string PlanCode { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "PLN";
        public DateTime IssuedAt { get; init; } = DateTime.UtcNow;
    }
}

