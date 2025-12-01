using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;

namespace FlowOps.Infrastructure.Sql.Reporting
{
    public interface ISqlReportingQueries
    {
        Task<CustomerReportSqlResponse?> GetCustomerReportAsync(Guid customerId, CancellationToken ct = default);
        Task<IReadOnlyList<Guid>> GetActiveSubscriptionIdsAsync(Guid customerId, CancellationToken ct = default);
        Task<IReadOnlyList<SubscriptionSqlResponse>> GetByCustomerAsync(Guid customerId, string? status = null, CancellationToken ct = default);
        Task<SubscriptionSqlResponse?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken ct = default);
        Task<PagedResult<SubscriptionSqlResponse>> GetByCustomerPagedAsync(
            Guid customerId,
            int page,
            int pageSize,
            string? orderBy = null,
            string? orderDirection = null,
            string? status = null,
            CancellationToken ct = default);
        Task<SubscriptionStatusSummaryResponse> GetStatusSummaryAsync(
            Guid customerId,
            CancellationToken ct = default);
    }
}
