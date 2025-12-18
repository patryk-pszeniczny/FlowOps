using FlowOps.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence
{
    public sealed class EfUnitOfWork : IUnitOfWork
    {
        private readonly FlowOpsDbContext _dbContext;

        public EfUnitOfWork(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async ct =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

                await action(ct);
                await _dbContext.SaveChangesAsync(ct);

                await transaction.CommitAsync(ct);
            }, cancellationToken);
        }

    }
}