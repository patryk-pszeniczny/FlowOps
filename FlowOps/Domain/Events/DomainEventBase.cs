using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.Domain.Events
{
    public abstract record DomainEventBase : IDomainEvent
    {
        protected DomainEventBase()
        {
            OccurredOn = DateTime.UtcNow;
        }
        public DateTime OccurredOn { get; }
    }
}
