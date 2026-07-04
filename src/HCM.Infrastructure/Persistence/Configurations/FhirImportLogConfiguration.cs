using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class FhirImportLogConfiguration : IEntityTypeConfiguration<FhirImportLog>
{
    public void Configure(EntityTypeBuilder<FhirImportLog> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();

        builder.Property(f => f.ResourceType).IsRequired().HasMaxLength(50);
        builder.Property(f => f.SourceSystem).HasMaxLength(100);
        builder.Property(f => f.FhirResourceId).HasMaxLength(200);
        builder.Property(f => f.InternalId).HasMaxLength(50);
        builder.Property(f => f.Status).IsRequired().HasMaxLength(20);
        builder.Property(f => f.ErrorMessage).HasColumnType("nvarchar(max)");
        builder.Property(f => f.ImportedBy).HasMaxLength(100);
        builder.Property(f => f.RawPayload).HasColumnType("nvarchar(max)");

        // No FK to Users — survives user deletion
        builder.HasIndex(f => f.ImportedAt);
        builder.HasIndex(f => new { f.ResourceType, f.Status });
    }
}
