using System.ComponentModel.DataAnnotations;

namespace FlowOps.Infrastructure.Sql
{
    public class IntegrationEventEntity
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(256)]
        public string TypeName { get; set; } = default!;

        public DateTime OccurredAt { get; set; }

        public int Version { get; set; }

        public string PayLoadJson { get; set; } = default!;
    }
}
