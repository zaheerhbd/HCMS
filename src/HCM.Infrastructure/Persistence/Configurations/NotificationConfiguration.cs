using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).UseIdentityColumn();

        builder.Property(n => n.UserId).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.EntityType).HasMaxLength(50);
        builder.Property(n => n.EntityId).HasMaxLength(100);

        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
    }
}
