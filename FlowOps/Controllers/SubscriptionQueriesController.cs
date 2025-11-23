using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Domain.Subscriptions;
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
            if(!_repo.TryGet(id, out var subscription) || subscription is null)
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
    }
}
