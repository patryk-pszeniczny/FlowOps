using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Contracts.Response;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Diagnostics
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

            var response = events.Select(ToResponse);

            _logger.LogInformation(
                "Returned {Count} events from event store.",
                events.Count);
            return Ok(response);
        }
        [HttpGet("events/{eventId:guid}")]
        public async Task<ActionResult<DetailedEventResponse>> GetById(Guid eventId, CancellationToken ct)
        {
            var events = await _eventStore.GetAllAsync(ct);
            var ev = events.FirstOrDefault(e => e.Id == eventId);
            if(ev is null)
            {
                return NotFound(new
                {
                    message = "Event not found.",
                    eventId
                });
            }
            var detailed = new DetailedEventResponse
            {
                Id = ev.Id,
                Type = ev.GetType().FullName ?? ev.GetType().Name,
                OccurredOn = ev.OccurredOn,
                Version = ev.Version,
                Payload = ev
            };
            return Ok(detailed);
        }
        [HttpGet("events/search")]
        public async Task<ActionResult<IEnumerable<EventResponse>>> Search(
            [FromQuery] string? type,
            [FromQuery] DateTime? since,
            [FromQuery] DateTime? until,
            [FromQuery] int take = 200,
            CancellationToken ct = default)
        {
            var items = await _eventStore.GetAllAsync(ct);
            if(!string.IsNullOrWhiteSpace(type))
            {
                items = items
                    .Where(e =>
                        string.Equals(e.GetType().FullName, type, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(e.GetType().Name, type, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
            }
            if (since.HasValue)
            {
                items = items.Where(e => e.OccurredOn >= since.Value).ToArray();
            }
            if (until.HasValue)
            {
                items = items.Where(e => e.OccurredOn <= until.Value).ToArray();
            }
            var sanitizedTake = Math.Clamp(take, 1, 500);
            var response = items
                .OrderByDescending(e => e.OccurredOn)
                .ThenByDescending(e => e.Version)
                .Take(sanitizedTake)
                .Select(ToResponse)
                .ToArray();

            return Ok(response);
        }
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<object>>> GetTypes(CancellationToken ct)
        {
            var items = await _eventStore.GetAllAsync(ct);

            var grouped = items
                .GroupBy(e => e.GetType().Name)
                .Select(g => new
                {
                    type = g.Key,
                    count = g.Count(),
                    firstSeen = g.Min(e => e.OccurredOn),
                    lastSeen = g.Max(e => e.OccurredOn)
                })
                .OrderByDescending(g => g.lastSeen)
                .ToArray();
            return Ok(grouped);
        }
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats(CancellationToken ct)
        {
            var events = await _eventStore.GetAllAsync(ct);
            var stats = new
            {
                total = events.Count,
                earliest = events.Min(e => (DateTime?)e.OccurredOn),
                latest = events.Max(e => (DateTime?)e.OccurredOn),
                byVersion = events
                    .GroupBy(e => e.Version)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            return Ok(stats);
        }
        private static EventResponse ToResponse(IntegrationEvent e) => new()
        {
            Id = e.Id,
            Type = e.GetType().FullName ?? e.GetType().Name,
            OccurredOn = e.OccurredOn,
            Version = e.Version
        };
    }
    public sealed class DetailedEventResponse : EventResponse
    {
        public object Payload { get; init; } = default!;
    }
}
