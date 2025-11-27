namespace FlowOps.Contracts.Response
{
    public sealed record SubscriptionSqlResponse
    (
        Guid SubscriptionId,
        Guid CustomerId,
        string PlanCode,
        string Status,
        DateTime ActivatedAt,
        DateTime? SuspendedAt,
        DateTime? ResumedAt,
        DateTime? CancelledAt
    );
    
}
