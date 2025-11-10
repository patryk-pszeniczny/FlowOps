using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var ev = new SubscriptionActivatedEvent
            {
                SubscriptionId = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                PlanCode = request.PlanCode
            };
            await _eventBus.PublishAsync(ev);

            return Ok(new
            {
                message = "Subscription created and event published.",
                subscriptionId = ev.SubscriptionId
            });
        }
    }
}
