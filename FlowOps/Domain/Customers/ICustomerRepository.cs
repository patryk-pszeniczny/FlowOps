namespace FlowOps.Domain.Customers
{
    public interface ICustomerRepository
    {
        Task AddAsync(Customer customer, CancellationToken ct = default);

        Task<bool> ExistsByTaxIdAsync(string taxId, CancellationToken ct = default);

        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

        Task<Customer?> GetByIdAsync(Guid id, bool asNoTracking = false, CancellationToken ct = default);

        Task<IReadOnlyList<Customer>> ListAsync(int take, CancellationToken ct = default);
    }
}