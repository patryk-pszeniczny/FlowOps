using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.Events
{
    public class SubscriptionActivatedEvent : IntegrationEvent
    {
        public Guid SubscriptionId { get; init; }
        public Guid CustomerId { get; init; }
        public string PlanCode { get; init; } = string.Empty;
    }
}
