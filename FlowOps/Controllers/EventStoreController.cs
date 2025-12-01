using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Contracts.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/event-store")]
    public class EventStoreController : ControllerBase
    {
        private readonly IIntegrationEventStore _eventStore;
        private readonly ILogger<EventStoreController> _logger;
        public EventStoreController(
            IIntegrationEventStore eventStore, 
            ILogger<EventStoreController> logger)
        {
            _eventStore = eventStore;
            _logger = logger;
        }
        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll(CancellationToken ct)
        {
            var events = await _eventStore.GetAllAsync(ct);

            var response = events.Select(e => new EventResponse
            {
                Id = e.Id,
                Type = e.GetType().FullName ?? e.GetType().Name,
                OccurredOn = e.OccurredOn,
                Version = e.Version
            });
            
            _logger.LogInformation("Returned {Count} events from event store.",
                events.Count());
            return Ok(response);
        }
    }
}
