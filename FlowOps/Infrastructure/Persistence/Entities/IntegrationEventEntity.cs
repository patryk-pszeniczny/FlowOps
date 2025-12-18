namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class IntegrationEventEntity
    {
        public Guid Id { get; set; }

        public string TypeName { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }

        public int Version { get; set; }

        public string PayLoadJson { get; set; } = string.Empty;
    }
}
