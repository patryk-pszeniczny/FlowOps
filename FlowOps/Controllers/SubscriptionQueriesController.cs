using FlowOps.Application.Subscriptions.Queries;
using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [Route("api/subscriptions")]
    [ApiController]
    public class SubscriptionQueriesController : ControllerBase
    {
        private readonly SubscriptionQueries _queries;
        public SubscriptionQueriesController(SubscriptionQueries queries)
        {
            _queries = queries;
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var response = await _queries.GetDetailsAsync(id, ct);
            return Ok(response);
        }
        [HttpGet("by-customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<SubscriptionListItem>>> GetByCustomerId(Guid customerId, CancellationToken ct)
        {
            var items = await _queries.GetByCustomerAsync(customerId, ct);
            return Ok(items);
        }
        [HttpGet("sql/by-customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<SubscriptionSqlResponse>>> GetByCustomerSql(
            Guid customerId,
            [FromQuery] string? status,
            CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                var allowedStatus = status.Trim();
                if (allowedStatus is not ("Active" or "Suspended" or "Cancelled"))
                {
                    return BadRequest("Invalid status filter. Allowed values are: Active, Suspended, Cancelled.");
                }
                status = allowedStatus;
            }
            var items = await _queries.GetByCustomerAsync(customerId, status, ct);
            return Ok(items);
        }
        [HttpGet("{subscriptionId:guid}")]
        public async Task<ActionResult<SubscriptionSqlResponse>> GetByIdSql(
            Guid subscriptionId,
            CancellationToken ct)
        {
            var item = await _queries.GetByIdAsync(subscriptionId, ct);
            if (item is null)
            {
                throw new KeyNotFoundException($"Subscription {subscriptionId} not found in SQL.");
            }
            return Ok(item);
        }
        [HttpGet("sql/by-customer/{customerId:guid}/paged")]
        public async Task<ActionResult<PagedResult<SubscriptionSqlResponse>>> GetByCustomerSqlPaged(
            Guid customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? orderBy = null,
            [FromQuery] string? orderDirection = null,
            [FromQuery] string? status = null,
            CancellationToken ct = default)
        {
            if (page <= 0)
            {
                return BadRequest("Page number must be greater than 0.");
            }
            if (pageSize <= 0 || pageSize > 200)
            {
                return BadRequest("Page size must be between 1 and 200.");
            }
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                var order = orderBy.Trim();
                if (order is not ("ActivatedAt" or "Status"))
                {
                    return BadRequest("Invalid orderBy value. Allowed values are: ActivatedAt, Status.");
                }
                orderBy = order;
            }
            if (!string.IsNullOrWhiteSpace(orderDirection))
            {
                var direction = orderDirection.Trim().ToUpperInvariant();
                if (direction is not ("ASC" or "DESC"))
                {
                    return BadRequest("Invalid orderDirection value. Allowed values are: ASC, DESC.");
                }
                orderDirection = direction;
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusTrimmed = status.Trim();
                if (statusTrimmed is not ("Active" or "Suspended" or "Cancelled"))
                {
                    return BadRequest("Invalid status filter. Allowed values are: Active, Suspended, Cancelled.");
                }
                status = statusTrimmed;
            }
            var pagedResult = await _queries.GetByCustomerPagedAsync(
                customerId,
                page,
                pageSize,
                orderBy,
                orderDirection,
                status,
                ct);
            return Ok(pagedResult);
        }

        [HttpGet("sql/by-customer/{customerId:guid}/status-summary")]
        public async Task<ActionResult<SubscriptionStatusSummaryResponse>> GetStatusSummarySql(
            Guid customerId,
            CancellationToken ct)
        {
            var summary = await _queries.GetStatusSummarySqlAsync(customerId, ct);
            return Ok(summary);
        }
    }
}
