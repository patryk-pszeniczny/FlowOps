namespace FlowOps.Contracts.Item
{
    public sealed record SubscriptionListItem
    (
        Guid Id,
        string PlanCode,
        string Status
    );
}
