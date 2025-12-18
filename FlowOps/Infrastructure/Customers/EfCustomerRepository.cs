using FlowOps.Domain.Customers;
using FlowOps.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Customers
{
    public sealed class EfCustomerRepository : ICustomerRepository
    {
        private readonly FlowOpsDbContext _dbContext;
        public EfCustomerRepository(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddAsync(Customer customer, CancellationToken ct = default)
        {
            var entity = new CustomerEntity
            {
                CustomerId = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                TaxId = customer.TaxId,
                CreatedAt = customer.CreatedAt
            };
            _dbContext.Customers.Add(entity);
            try
            {
                await _dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Customer create failed due to database constraint (TaxId/Email must by unique).", ex);
            }
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

        public async Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken ct = default)
        {
            var entity = await _dbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId, ct);

            return entity is null
                ? null
                : MapToDomain(entity);
        }

        public async Task<IReadOnlyList<Customer>> ListAsync(int take, CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 200);

            var entities = await _dbContext.Customers
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .ToListAsync(ct);
            return entities
                .Select(MapToDomain)
                .ToList();
        }
        private static Customer MapToDomain(CustomerEntity entity)
        {
            return Customer.FromExisting(
                id: entity.CustomerId,
                name: entity.Name,
                taxId: entity.TaxId,
                email: entity.Email,
                createdAtUtc: entity.CreatedAt);
        }

    }
}
