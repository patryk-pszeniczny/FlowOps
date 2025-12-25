using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.Domain.Events
{
    public abstract record DomainEventBase : IDomainEvent
    {
        protected DomainEventBase()
        {
            OccurredOn = DateTime.UtcNow;
        }
        protected DomainEventBase(DateTime occurredOn)
        {
            OccurredOn = occurredOn;
        }
        public DateTime OccurredOn { get; }
    }
}
