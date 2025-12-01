
using FlowOps.Infrastructure.Sql;

namespace FlowOps.Infrastructure.Idempotency
{
    public sealed class EfCoreIdempotencyStore : IIdempotencyStore
    {
        private readonly FlowOpsDbContext _dbContext;
        public EfCoreIdempotencyStore(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public void Set(string key, Guid subscriptionId)
        {
            var existing = _dbContext.IdempotencyKeys.Find(key);
            if(existing is not null)
            {
                return;
            }
            var entity = new IdempotencyKeyEntity
            {
                Key = key,
                SubscriptionId = subscriptionId
            };
            _dbContext.IdempotencyKeys.Add(entity);
            _dbContext.SaveChanges();
        }

        public bool TryGet(string key, out Guid subscriptionId)
        {
            var entity = _dbContext.IdempotencyKeys.Find(key);
            if (entity is null)
            {
                subscriptionId = Guid.Empty;
                return false;
            }
            subscriptionId = entity.SubscriptionId;
            return true;

        }
    }
}
