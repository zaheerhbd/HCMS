using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CaseTagConfiguration : IEntityTypeConfiguration<CaseTag>
{
    public void Configure(EntityTypeBuilder<CaseTag> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(ct => ct.Name).IsUnique();

        builder.Property(ct => ct.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(ct => ct.Cases)
            .WithOne(cct => cct.Tag)
            .HasForeignKey(cct => cct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
