namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class InboxMessage
    {
        public string Consumer { get; set; } = string.Empty;

        public Guid EventId { get; set; }

        public DateTime ProcessedAt { get; set; }
    }
}
