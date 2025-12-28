namespace FlowOps.Contracts.Response
{
    public class InvoiceDetailsResponse
    {
        public Guid InvoiceId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid SubscriptionId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "issued";
        public DateTime? PaidAt { get; set; }
    }
}
