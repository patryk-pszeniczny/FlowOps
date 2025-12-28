using FlowOps.Application.Reporting;
using FlowOps.Contracts.Response;
using FlowOps.Events;
using FlowOps.Services.Replay;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Reporting
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingQueries _queries;
        private readonly EventRecorder _recorder;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportingQueries queries, EventRecorder recorder, ILogger<ReportsController> logger)
        {
            _queries = queries;
            _recorder = recorder;
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
        [HttpGet("customers/{customerId:guid}/timeline")]
        public ActionResult<IEnumerable<object>> GetCustomerTimeline(Guid customerId)
        {
            var timeline = _recorder
                .Snapshot()
                .Where(ev => ev switch
                {
                    InvoiceIssuedEvent issued => issued.CustomerId == customerId,
                    InvoicePaidEvent paid => paid.CustomerId == customerId,
                    SubscriptionActivatedEvent activated => activated.CustomerId == customerId,
                    SubscriptionCancelledEvent cancelled => cancelled.CustomerId == customerId,
                    SubscriptionSuspendedEvent suspended => suspended.CustomerId == customerId,
                    SubscriptionResumedEvent resumed => resumed.CustomerId == customerId,
                    _ => false
                })
                .OrderBy(ev => ev.OccurredOn)
                .ThenBy(ev => ev.Version)
                .Select(ev => new
                {
                    type = ev.GetType().Name,
                    ev.Id,
                    ev.OccurredOn,
                    ev.Version
                })
                .ToList();

            return Ok(timeline);
        }
    }
}
