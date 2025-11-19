using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Contracts;
using Microsoft.AspNetCore.Mvc;
using FlowOps.Domain.Subscriptions;
using FlowOps.Application.Subscriptions;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionCommandService _service;
        public SubscriptionsController(SubscriptionCommandService service)
        {
            _service = service;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request)
        {
            var id = await _service.CreateAsync(request.CustomerId, request.PlanCode, DateTime.UtcNow);
            return Ok(
                new
                {
                    message = "Subscription created and event published.",
                    subscriptionId = id
                }
            );
        }
    }
}
