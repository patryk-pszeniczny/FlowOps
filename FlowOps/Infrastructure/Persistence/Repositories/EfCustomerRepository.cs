using FlowOps.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence.Repositories
{
    public sealed class EfCustomerRepository : ICustomerRepository
    {
        private readonly FlowOpsDbContext _dbContext;

        public EfCustomerRepository(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task AddAsync(Customer customer, CancellationToken ct = default)
        {
            _dbContext.Customers.Add(customer);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        {
            return await _dbContext.Customers
                .AsNoTracking()
                .AnyAsync(c => c.Email == email, ct);
        }

        public async Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct = default)
        {
            return await _dbContext.Customers
                .AsNoTracking()
                .AnyAsync(c => c.TaxId == taxId, ct);
        }

        public async Task<Customer?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken ct = default)
        {
            var query = asNoTracking
                ? _dbContext.Customers.AsNoTracking()
                : _dbContext.Customers.AsTracking();

            return await query.FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<IReadOnlyList<Customer>> ListAsync(int take, CancellationToken ct = default)
        {
            take = Math.Clamp(take <= 0 ? 20 : take, 1, 200);

            var customers = await _dbContext.Customers
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .ToListAsync(ct);

            return customers;
        }
    }
}