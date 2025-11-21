using FlowOps.Contracts.Response;
using FlowOps.Domain.Subscriptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [Route("api/subscriptions")]
    [ApiController]
    public class SubscriptionQueriesController : ControllerBase
    {
        private readonly ISubscriptionRepository _repo;
        public SubscriptionQueriesController(ISubscriptionRepository repo)
        {
            _repo = repo;
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
    }
}
