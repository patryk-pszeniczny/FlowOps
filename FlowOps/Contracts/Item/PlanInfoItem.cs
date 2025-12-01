namespace FlowOps.Contracts.Item
{
    public sealed record PlanInfoItem
    (
        string Code,
        string Name,
        decimal Price,
        string Currency
    );
}
