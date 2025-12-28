namespace FlowOps.Contracts.Request
{
    public sealed class IssueInvoiceRequest
    {
        public Guid InvoiceId { get; init; }
        public Guid CustomerId { get; init; }
        public Guid SubscriptionId { get; init; }
        public string PlanCode { get; init; } = string.Empty;

        public decimal? Amount { get; init; }
        public string? Currency { get; init; }
    }
}
