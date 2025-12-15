using FlowOps.Infrastructure.Sql.Reporting.Customer;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Customer
{
    [ApiController]
    [Route("api/reporting/customers")]
    public sealed class CustomerDirectoryController : ControllerBase
    {
        private readonly CustomerDirectoryQueries _queries;

        public CustomerDirectoryController(CustomerDirectoryQueries queries)
        {
            _queries = queries;
        }
        [HttpGet("{customerId:guid}")]
        public async Task<IActionResult> Get(Guid customerId, CancellationToken ct)
        {
            var customer = await _queries.GetAsync(customerId, ct);
            if (customer is null)
            {
                return NotFound();
            }
            return Ok(customer);
        }
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int take = 20, CancellationToken ct = default)
        {
            var customers = await _queries.SearchAsync(q, take, ct);
            return Ok(customers);
        }
    }
}
