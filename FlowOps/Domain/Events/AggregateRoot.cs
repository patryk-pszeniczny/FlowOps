using FlowOps.BuildingBlocks.Domain.Events;

namespace FlowOps.Domain.Events
{
    public interface IHasDomainEvents
    {
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void ClearDomainEvents();
    }
    public abstract class AggregateRoot : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            if (domainEvent is null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }
            _domainEvents.Add(domainEvent);
        }
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}