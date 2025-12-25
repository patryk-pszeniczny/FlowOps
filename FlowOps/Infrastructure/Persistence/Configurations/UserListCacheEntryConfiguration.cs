using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowOps.Infrastructure.Persistence.Configurations
{
    internal sealed class UserListCacheEntryConfiguration : IEntityTypeConfiguration<UserListCacheEntry>
    {
        public void Configure(EntityTypeBuilder<UserListCacheEntry> builder)
        {
            builder.ToTable("UserListCaches");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.CacheKey)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Payload)
                .IsRequired();

            builder.Property(x => x.CachedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.CacheKey })
                .IsUnique();

            builder.HasIndex(x => x.ExpiresAt);
        }
    }
}