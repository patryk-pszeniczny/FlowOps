namespace FlowOps.Contracts.Response
{
    public sealed record SubscriptionDetailsResponse
    (
        Guid id,
        Guid CustomerId,
        string PlanCode,
        string Status
    );
}
