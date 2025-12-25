using System.ComponentModel.DataAnnotations;

namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class SubscriptionDraft
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? CustomerId { get; set; }

        [MaxLength(100)]
        public string? PlanCode { get; set; }

        [Required]
        public string Payload { get; set; } = string.Empty;

        public DateTime LastUpdatedAt { get; set; }
    }
}
