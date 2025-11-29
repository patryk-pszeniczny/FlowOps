using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Sql.Reporting;
using FlowOps.Reports.Stores;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [Route("api/subscriptions")]
    [ApiController]
    public class SubscriptionQueriesController : ControllerBase
    {
        private readonly ISubscriptionRepository _repo;
        private readonly IReportingStore _store;
        public SubscriptionQueriesController(ISubscriptionRepository repo,
            IReportingStore store)
        {
            _repo = repo;
            _store = store;
        }
        [HttpGet("{id:guid}")]
        public IActionResult GetById(Guid id)
        {
            if (!_repo.TryGet(id, out var subscription) || subscription is null)
            {
                throw new KeyNotFoundException($"Subscription {id} not found.");
            }
            var response = new SubscriptionDetailsResponse(
                subscription.Id,
                subscription.CustomerId,
                subscription.PlanCode,
                subscription.Status.ToString()
            );
            return Ok(response);
        }
        [HttpGet("by-customer/{customerId:guid}")]
        public ActionResult<IEnumerable<SubscriptionListItem>> GetByCustomerId(Guid customerId)
        {
            var report = _store.GetOrAdd(customerId);

            var items = report.ActiveSubscriptionIds
                .Select(id => _repo.TryGet(id, out var sub) ? sub : null)
                .Where(sub => sub is not null)
                .Select(sub => new SubscriptionListItem
                (
                    sub!.Id,
                    sub.PlanCode,
                    sub.Status.ToString()
                ))
                .OrderBy(x => x.PlanCode)
                .ToList();
            return Ok(items);
        }
        [HttpGet("sql/by-customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<SubscriptionSqlResponse>>> GetByCustomerSql(
            Guid customerId,
            [FromQuery] string? status,
            [FromServices] ISqlReportingQueries queries,
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
            var items = await queries.GetByCustomerAsync(customerId, status, ct);
            return Ok(items);
        }
        [HttpGet("sql/{subscriptionId:guid}")]
        public async Task<ActionResult<SubscriptionSqlResponse>> GetByIdSql(
            Guid subscriptionId,
            [FromServices] ISqlReportingQueries queries,
            CancellationToken ct)
        {
            var item = await queries.GetSubscriptionByIdAsync(subscriptionId, ct);
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
            [FromServices] ISqlReportingQueries queries = null!,
            CancellationToken ct = default)
        {
            if(page <= 0)
            {
                return BadRequest("Page number must be greater than 0.");
            }
            if(pageSize <= 0 || pageSize > 200)
            {
                return BadRequest("Page size must be between 1 and 200.");
            }
            if (!string.IsNullOrWhiteSpace(orderBy)){
                var order = orderBy.Trim();
                if(order is not ("ActivatedAt" or "Status"))
                {
                    return BadRequest("Invalid orderBy value. Allowed values are: ActivatedAt, Status.");
                }
                orderBy = order;
            }
            if (!string.IsNullOrWhiteSpace(orderDirection)){
                var direction = orderDirection.Trim().ToUpperInvariant();
                if(direction is not ("asc" or "desc"))
                {
                    return BadRequest("Invalid orderDirection value. Allowed values are: asc, desc.");
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
            var pagedResult = await queries.GetByCustomerPagedAsync(
                customerId,
                page,
                pageSize,
                orderBy,
                orderDirection,
                status,
                ct);
            return Ok(pagedResult);
        }
    }
}
