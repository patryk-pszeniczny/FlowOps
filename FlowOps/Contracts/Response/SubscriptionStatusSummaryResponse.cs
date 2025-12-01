namespace FlowOps.Contracts.Response
{
    public sealed record SubscriptionStatusSummaryResponse(
        Guid CustomerId,
        int Active,
        int Suspended,
        int Cancelled,
        int Total
    );
}

