using System.ComponentModel.DataAnnotations;

namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class UserPreference
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Value { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }
}
