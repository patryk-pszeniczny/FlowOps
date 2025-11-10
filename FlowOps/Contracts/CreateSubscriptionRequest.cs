namespace FlowOps.Contracts
{
    public sealed class CreateSubscriptionRequest
    {
        public Guid CustomerId { get; init; }
        public string PlanCode { get; init; } = string.Empty;
    }
}
