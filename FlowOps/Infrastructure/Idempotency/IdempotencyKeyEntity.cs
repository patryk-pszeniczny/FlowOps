using System.ComponentModel.DataAnnotations;

namespace FlowOps.Infrastructure.Idempotency
{
    public class IdempotencyKeyEntity
    {
        [Key]
        [MaxLength(200)]
        public string Key { get; set; } = default!;

        public Guid SubscriptionId { get; set; }
    }
}
