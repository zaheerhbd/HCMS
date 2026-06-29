using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CaseCaseTagConfiguration : IEntityTypeConfiguration<CaseCaseTag>
{
    public void Configure(EntityTypeBuilder<CaseCaseTag> builder)
    {
        builder.HasKey(cct => new { cct.CaseId, cct.TagId });

        builder.HasOne(cct => cct.Case)
            .WithMany(c => c.CaseTags)
            .HasForeignKey(cct => cct.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cct => cct.Tag)
            .WithMany(ct => ct.Cases)
            .HasForeignKey(cct => cct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
