using FlowOps.Contracts.Response;
using FlowOps.Infrastructure.Sql.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ISqlReportingQueries _queries;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ISqlReportingQueries queries, ILogger<ReportsController> logger)
        {
            _queries = queries;
            _logger = logger;
        }

        [HttpGet("customers/{customerId:guid}")]
        public async Task<ActionResult<CustomerReportSqlResponse>> GetCustomerReport(Guid customerId, CancellationToken ct)
        {
            var result = await _queries.GetCustomerReportAsync(customerId, ct);

            if (result is null)
            {
                _logger.LogInformation("Customer report not found in SQL. CustomerId={CustomerId}", customerId);
                return NotFound(new { message = "Customer report not found", customerId });
            }

            return Ok(result);
        }

        [HttpGet("customers/{customerId:guid}/active-subscriptions")]
        public async Task<ActionResult<IEnumerable<Guid>>> GetActiveSubscriptionIds(Guid customerId, CancellationToken ct)
        {
            var ids = await _queries.GetActiveSubscriptionIdsAsync(customerId, ct);
            var ordered = ids.OrderBy(x => x).ToArray();
            return Ok(ordered);
        }
    }
}
