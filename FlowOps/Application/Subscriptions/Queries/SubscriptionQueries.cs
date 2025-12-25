using AutoMapper;
using FlowOps.Application.Reporting;
using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;
using FlowOps.Domain.Subscriptions;

namespace FlowOps.Application.Subscriptions.Queries
{
    public sealed class SubscriptionQueries
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IReportingQueries _reportingQueries;
        private readonly IMapper _mapper;
        public SubscriptionQueries(
            ISubscriptionRepository repository,
            IReportingQueries reportingQueries,
            IMapper mapper)
        {
            _repository = repository;
            _reportingQueries = reportingQueries;
            _mapper = mapper;
        }
        public async Task<SubscriptionDetailsResponse> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(id, asNoTracking: true, ct: ct)
                ?? throw new KeyNotFoundException($"Subscription with ID '{id}' not found.");

            return _mapper.Map<SubscriptionDetailsResponse>(subscription);
        }
        public async Task<IReadOnlyList<SubscriptionListItem>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        {
            var subscriptions = await _repository.GetByCustomerAsync(customerId, ct);
            var ordered = subscriptions.OrderBy(s => s.PlanCode);
            return _mapper.Map<IReadOnlyList<SubscriptionListItem>>(ordered);
        }
        public Task<IReadOnlyList<SubscriptionSqlResponse>> GetByCustomerAsync(Guid customerId, string? status, CancellationToken ct = default) =>
            _reportingQueries.GetByCustomerAsync(customerId, status, ct);

        public Task<SubscriptionSqlResponse?> GetByIdAsync(Guid subscriptionId, CancellationToken ct = default) =>
            _reportingQueries.GetSubscriptionByIdAsync(subscriptionId, ct);

        public Task<PagedResult<SubscriptionSqlResponse>> GetByCustomerPagedAsync(
            Guid customerId,
            int page,
            int pageSize,
            string? orderBy,
            string? orderDirection,
            string? status,
            CancellationToken ct = default) =>
            _reportingQueries.GetByCustomerPagedAsync(customerId, page, pageSize, orderBy, orderDirection, status, ct);

        public Task<SubscriptionStatusSummaryResponse> GetStatusSummarySqlAsync(Guid customerId, CancellationToken ct = default) =>
            _reportingQueries.GetStatusSummaryAsync(customerId, ct);

    }
}
