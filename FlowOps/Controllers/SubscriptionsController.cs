using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Contracts;
using Microsoft.AspNetCore.Mvc;
using FlowOps.Domain.Subscriptions;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly IEventBus _eventBus;

        public SubscriptionsController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                var subscription = Subscription.Create(request.CustomerId, request.PlanCode);
                var ev = subscription.Activate(DateTime.UtcNow);

                await _eventBus.PublishAsync(ev);

                return Ok(new
                {
                    message = "Subscription created and event published.",
                    subscriptionId = ev.SubscriptionId
                });
            }
            catch(ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
