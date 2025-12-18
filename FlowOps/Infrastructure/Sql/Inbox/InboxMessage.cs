using System.ComponentModel.DataAnnotations;

namespace FlowOps.Infrastructure.Sql.Inbox
{
    public sealed class InboxMessage
    {
        [Key]
        public Guid EventId { get; set; }

        [MaxLength(200)]
        public string Consumer { get; set; } = null!;

        public DateTime ProcessedAt { get; set; }

    }
}
