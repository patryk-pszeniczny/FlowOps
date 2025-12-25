using AutoMapper;
using AutoMapper.QueryableExtensions;
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
        private readonly AutoMapper.IConfigurationProvider  _mapperConfiguration;
        public EfReportingQueries(FlowOpsDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapperConfiguration = mapper.ConfigurationProvider;
        }

        public async Task<CustomerReportSqlResponse?> GetCustomerReportAsync(Guid customerId, CancellationToken ct = default)
        {
            return await _dbContext.CustomerReports
                .AsNoTracking()
                .Where(r => r.CustomerId == customerId)
                .ProjectTo<CustomerReportSqlResponse>(_mapperConfiguration)
                .FirstOrDefaultAsync(ct);
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

            return await query
                .OrderByDescending(s => s.ActivatedAt ?? DateTime.MinValue)
                .ProjectTo<SubscriptionSqlResponse>(_mapperConfiguration)
                .ToListAsync(ct);
        }

        public async Task<SubscriptionSqlResponse?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken ct = default)
        {
            return await _dbContext.Subscriptions
                    .AsNoTracking()
                    .Where(s => s.Id == subscriptionId)
                    .ProjectTo<SubscriptionSqlResponse>(_mapperConfiguration)
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
                .ProjectTo<SubscriptionSqlResponse>(_mapperConfiguration)
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

            var active    = grouped.FirstOrDefault(g => g.Status == SubscriptionStatus.Active)?.Count ?? 0;
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

      
    }
}