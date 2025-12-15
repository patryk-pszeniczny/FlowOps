using FlowOps.Application.Customer;
using FlowOps.Contracts.Request.Customers;
using FlowOps.Infrastructure.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Controllers.Customer
{
    [ApiController]
    [Route("api/customer")]
    public sealed class CustomerController : ControllerBase
    {
        private readonly FlowOpsDbContext _database;
        private readonly CustomerCommandService _command;
        private readonly ILogger<CustomerController> _logger;
        public CustomerController(FlowOpsDbContext database, 
            CustomerCommandService command,
            ILogger<CustomerController> logger)
        {
            _database = database;
            _command = command;
            _logger = logger;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
        {
            try
            {
                var entity = await _command.CreateAsync(request, ct);

                return CreatedAtAction(nameof(GetById), new { 
                    customerId = entity.CustomerId 
                }, new{
                    entity.CustomerId,
                    entity.Name,
                    entity.TaxId,
                    entity.Email,
                    entity.CreatedAt
                });
            }
            catch(InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "CreateCustomer rejected.");
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpGet("{customerId:guid}")]
        public async Task<IActionResult> GetById(Guid customerId, CancellationToken ct)
        {
            var customer = await _database.Customers
                .AsNoTracking()
                .Where(c => c.CustomerId == customerId)
                .Select(c => new
                {
                    c.CustomerId,
                    c.Name,
                    c.TaxId,
                    c.Email,
                    c.CreatedAt
                })
                .FirstOrDefaultAsync(ct);
            if (customer is null)
            {
                return NotFound(new
                {
                    message = "Customer not found."
                });
            }
            return Ok(customer);
        }
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int take, CancellationToken ct = default)
        {
            take = Math.Clamp(take, 1, 200);

            var customers = await _database.Customers
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Take(take)
                .Select(c => new
                {
                    c.CustomerId,
                    c.Name,
                    c.TaxId,
                    c.Email,
                    c.CreatedAt
                })
                .ToListAsync(ct);
            return Ok(customers);
        }
    }
}
