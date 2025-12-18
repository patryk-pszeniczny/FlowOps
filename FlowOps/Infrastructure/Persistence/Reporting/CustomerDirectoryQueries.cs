using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence.Reporting
{
    public sealed class CustomerDirectoryQueries
    {
        private readonly FlowOpsDbContext _database;
        private readonly ILogger<CustomerDirectoryQueries> _logger;

        public CustomerDirectoryQueries(
            FlowOpsDbContext database,
            ILogger<CustomerDirectoryQueries> logger)
        {
            _database = database;
            _logger = logger;
        }
        public async Task<CustomerDirectoryEntry?> GetAsync(Guid customerId, CancellationToken ct)
        {
            return await _database.Set<CustomerDirectoryEntry>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);
        }
        public async Task<IReadOnlyList<CustomerDirectoryEntry>> SearchAsync(string? q, int take, CancellationToken ct)
        {
            if (take <= 0) take = 20;
            if (take > 100) take = 100;

            var query = _database.Set<CustomerDirectoryEntry>().AsNoTracking();

            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

            if (q is not null)
            {
                query = query.Where(c =>
                    c.Name.Contains(q) ||
                    c.Email != null && c.Email.Contains(q) ||
                    c.TaxId != null && c.TaxId.Contains(q)
                );
            }
            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
        }
    }
}
