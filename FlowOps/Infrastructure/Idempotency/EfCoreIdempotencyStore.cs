
using FlowOps.Infrastructure.Sql;

namespace FlowOps.Infrastructure.Idempotency
{
    public sealed class EfCoreIdempotencyStore : IIdempotencyStore
    {
        private readonly FlowOpsDbContext _dbContext;
        private readonly ILogger<EfCoreIdempotencyStore> _logger;
        public EfCoreIdempotencyStore(FlowOpsDbContext dbContext, ILogger<EfCoreIdempotencyStore> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public void Set(string key, Guid subscriptionId)
        {
            try
            {
                var existing = _dbContext.IdempotencyKeys.Find(key);
                if (existing is not null)
                {
                    _logger.LogDebug(
                        "Idempotency key '{Key}' already exists with SubscriptionId = {SubscriptionId}. No action taken.",
                        key,
                        existing.SubscriptionId);
                    return;
                }
                var entity = new IdempotencyKeyEntity
                {
                    Key = key,
                    SubscriptionId = subscriptionId
                };
                _dbContext.IdempotencyKeys.Add(entity);
                _dbContext.SaveChanges();

                _logger.LogInformation(
                    "Stored idempotency key '{Key}' for SubscriptionId = {SubscriptionId}.",
                    key,
                    subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while storing idempotency key '{Key}' for SubscriptionId = {SubscriptionId}",
                    key,
                    subscriptionId);
                throw;
            }
            
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
