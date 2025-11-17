using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.Events
{
    public class InvoicePaidEvent : IntegrationEvent
    {
        public Guid InvoiceId { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SubscriptionId { get; init; }

        public decimal Amount { get; init; }
        public string Currency { get; init; } = "PLN";
        public DateTime PaidAt { get; init; } = DateTime.UtcNow;

        public string? PaymentMethod { get; init; }
        public string? TransactionId { get; init; }
    }
}
