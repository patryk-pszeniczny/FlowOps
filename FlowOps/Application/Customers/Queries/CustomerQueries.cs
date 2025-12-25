using AutoMapper;
using FlowOps.Domain.Customers;
namespace FlowOps.Application.Customers.Queries
{
    public sealed class CustomerQueries
    {
        private readonly ICustomerRepository _repository;
        private readonly IMapper _mapper;
        public CustomerQueries(ICustomerRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<CustomerDto?> GetAsync(Guid customerId, CancellationToken ct = default)
        {
            var customer = await _repository.GetByIdAsync(customerId, asNoTracking: true, ct: ct);
            return customer is null
                ? null
                : _mapper.Map<CustomerDto>(customer);
        }
        public async Task<IReadOnlyList<CustomerDto>> ListAsync(int take, CancellationToken ct = default)
        {
            var customers = await _repository.ListAsync(take, ct);

            return _mapper.Map<IReadOnlyList<CustomerDto>>(customers);
        }
    }
}
