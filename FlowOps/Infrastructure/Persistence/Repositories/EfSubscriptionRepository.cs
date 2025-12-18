using FlowOps.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence.Repositories
{
    public sealed class EfSubscriptionRepository : ISubscriptionRepository
    {
        private readonly FlowOpsDbContext _dbContext;

        public EfSubscriptionRepository(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task AddAsync(Subscription subscription, CancellationToken ct = default)
        {
            _dbContext.Subscriptions.Add(subscription);
            return Task.CompletedTask;
        }

        public async Task<Subscription?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken ct = default)
        {
            var query = asNoTracking
                ? _dbContext.Subscriptions.AsNoTracking()
                : _dbContext.Subscriptions.AsTracking();

            return await query.FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<IReadOnlyList<Subscription>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        {
            var subscriptions = await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.ActivatedAt ?? DateTime.MinValue)
                .ToListAsync(ct);

            return subscriptions;
        }

    }
}