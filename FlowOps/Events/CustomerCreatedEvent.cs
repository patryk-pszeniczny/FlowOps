using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.Events
{
    public class CustomerCreatedEvent : IntegrationEvent
    {
        public Guid CustomerId { get; init; }
        public string Name { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? Email { get; set; }

        public DateTime CreatedAt { get; init; }
    }
}
