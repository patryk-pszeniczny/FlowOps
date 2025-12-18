using FlowOps.Application.Customer;
using FlowOps.Application.Customers.Commands;
using FlowOps.Application.Customers.Queries;
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
        private readonly CreateCustomerCommandHandler _command;
        private readonly CustomerQueries _queries;
        private readonly ILogger<CustomerController> _logger;
        public CustomerController(
            CreateCustomerCommandHandler command,
            CustomerQueries queries,
            ILogger<CustomerController> logger)
        {
            _command = command;
            _queries = queries;
            _logger = logger;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
        {
            try
            {
                var customer = await _command.HandleAsync(new CreateCustomerCommand(
                    request.Name,
                    request.TaxId,
                    request.Email
                ), ct);

                return CreatedAtAction(nameof(GetById), new { 
                    customerId = customer.CustomerId 
                }, new{
                    CustomerId = customer.Id,
                    customer.Name,
                    customer.TaxId,
                    customer.Email,
                    customer.CreatedAt
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
            var customer = await _queries.GetAsync(customerId, ct);
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
            var customers = await _queries.ListAsync(take, ct);
            return Ok(customers);
        }
    }
}
