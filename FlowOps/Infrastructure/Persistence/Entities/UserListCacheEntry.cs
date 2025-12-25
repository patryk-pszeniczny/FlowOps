using System.ComponentModel.DataAnnotations;

namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class UserListCacheEntry
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(150)]
        public string CacheKey { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        public DateTime CachedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
