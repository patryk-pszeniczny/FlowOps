using FlowOps.Application.Reporting;
using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;
using FlowOps.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowOps.Infrastructure.Persistence.Reporting
{
    public sealed class EfReportingQueries : IReportingQueries
    {
        private readonly FlowOpsDbContext _dbContext;
        public EfReportingQueries(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CustomerReportSqlResponse?> GetCustomerReportAsync(Guid customerId, CancellationToken ct = default)
        {
            var report = await _dbContext.CustomerReports
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.CustomerId == customerId, ct);

            if (report is null)
            {
                return null;
            }

            return new CustomerReportSqlResponse(
                report.CustomerId,
                report.ActiveSubscriptions,
                report.TotalInvoiced,
                report.TotalPaid);
        }

        public async Task<IReadOnlyList<Guid>> GetActiveSubscriptionIdsAsync(Guid customerId, CancellationToken ct = default)
        {
            return await _dbContext.ActiveSubscriptions
                .AsNoTracking()
                .Where(a => a.CustomerId == customerId)
                .OrderBy(a => a.SubscriptionId)
                .Select(a => a.SubscriptionId)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SubscriptionSqlResponse>> GetByCustomerAsync(Guid customerId, string? status = null, CancellationToken ct = default)
        {
            var query = _dbContext.Subscriptions
                .AsNoTracking()
                .Where(s => s.CustomerId == customerId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubscriptionStatus>(status, ignoreCase: true, out var parsed))
            {
                query = query.Where(s => s.Status == parsed);
            }

            var items = await query
                .OrderByDescending(s => s.ActivatedAt ?? DateTime.MinValue)
                .Select(SubscriptionProjection)
                .ToListAsync(ct);

            return items;
        }

        public async Task<SubscriptionSqlResponse?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken ct = default)
        {
            return await _dbContext.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.Id == subscriptionId)
                    .Select(SubscriptionProjection)
                    .FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResult<SubscriptionSqlResponse>> GetByCustomerPagedAsync(
            Guid customerId,
            int page,
            int pageSize,
            string? orderBy = null,
            string? orderDirection = null,
            string? status = null,
            CancellationToken ct = default)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var query = _dbContext.Subscriptions
                .AsNoTracking()
                .Where(s => s.CustomerId == customerId);

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubscriptionStatus>(status, ignoreCase: true, out var parsed))
            {
                query = query.Where(s => s.Status == parsed);
            }

            query = (orderBy?.Trim()) switch
            {
                "Status" => (orderDirection?.Trim().ToUpperInvariant()) switch
                {
                    "ASC" => query.OrderBy(s => s.Status).ThenBy(s => s.Id),
                    _ => query.OrderByDescending(s => s.Status).ThenBy(s => s.Id)
                },
                _ => (orderDirection?.Trim().ToUpperInvariant()) switch
                {
                    "ASC" => query.OrderBy(s => s.ActivatedAt ?? DateTime.MinValue).ThenBy(s => s.Id),
                    _ => query.OrderByDescending(s => s.ActivatedAt ?? DateTime.MinValue).ThenBy(s => s.Id)
                }
            };

            var totalCount = await query.CountAsync(ct);

            var skip = (page - 1) * pageSize;
            var items = await query
                .Skip(skip)
                .Take(pageSize)
                .Select(SubscriptionProjection)
                .ToListAsync(ct);

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            return new PagedResult<SubscriptionSqlResponse>(
                Items: items,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: totalPages);
        }

        public async Task<SubscriptionStatusSummaryResponse> GetStatusSummaryAsync(Guid customerId, CancellationToken ct = default)
        {
            var grouped = await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(s => s.CustomerId == customerId)
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var active = grouped.FirstOrDefault(g => g.Status == SubscriptionStatus.Active)?.Count ?? 0;
            var suspended = grouped.FirstOrDefault(g => g.Status == SubscriptionStatus.Suspended)?.Count ?? 0;
            var cancelled = grouped.FirstOrDefault(g => g.Status == SubscriptionStatus.Cancelled)?.Count ?? 0;

            var total = active + suspended + cancelled;

            return new SubscriptionStatusSummaryResponse(
                CustomerId: customerId,
                Active: active,
                Suspended: suspended,
                Cancelled: cancelled,
                Total: total);
        }

        private Expression<Func<Subscription, SubscriptionSqlResponse>> SubscriptionProjection =
            s => new SubscriptionSqlResponse(
                s.Id,
                s.CustomerId,
                s.PlanCode,
                s.Status.ToString(),
                s.ActivatedAt ?? DateTime.MinValue,
                s.SuspendedAt,
                s.ResumedAt,
                s.CancelledAt);
    }
}