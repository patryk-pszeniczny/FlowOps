namespace FlowOps.Application.Customers.Queries
{
    public sealed record CustomerDto(
        Guid Id,
        string Name,
        string? TaxId,
        string? Email,
        DateTime CreatedAt);
}
