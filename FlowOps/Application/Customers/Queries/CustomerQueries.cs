namespace FlowOps.Application.Customers.Queries
{
    public sealed class CustomerQueries
    {
        private readonly ICustomerRepostiory _repository;
        public CustomerQueries(ICustomerRepostiory repository)
        {
            _repository = repository;
        }
        public async Task<CustomerDto?> GetAsync(Guid customerId, CancellationToken ct = default)
        {
            var customer = await _repository.GetByIdAsync(customerId, ct);
            return customer is null
                ? null
                : Map(customer);
        }
        public async Task<IReadOnlyList<CustomerDto>> ListAsync(int take, CancellationToken ct = default)
        {
            var customers = await _repository.ListAsync(take, ct);

            return customers
                .Select(Map)
                .ToList();
        }
        private static CustomerDto Map(Customer customer)
            => new(
                customer.Id,
                customer.Name,
                customer.TaxId,
                customer.Email,
                customer.CreatedAt);
    }
}
