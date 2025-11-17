using System.ComponentModel.DataAnnotations;

namespace FlowOps.Contracts
{
    public sealed class CreateSubscriptionRequest
    {
        [Required]
        public Guid CustomerId { get; init; }
        [Required]
        [MinLength(2)]
        [MaxLength(32)]
        public string PlanCode { get; init; } = string.Empty;
    }
}
