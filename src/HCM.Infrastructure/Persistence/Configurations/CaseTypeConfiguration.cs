using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CaseTypeConfiguration : IEntityTypeConfiguration<CaseType>
{
    public void Configure(EntityTypeBuilder<CaseType> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Name)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(ct => ct.Name).IsUnique();

        builder.Property(ct => ct.Description)
            .HasMaxLength(500);

        builder.Property(ct => ct.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(ct => ct.Cases)
            .WithOne(c => c.CaseType)
            .HasForeignKey(c => c.CaseTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
