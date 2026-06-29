using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CaseNumber)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(c => c.CaseNumber).IsUnique();

        builder.Property(c => c.CurrentStatus)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue(CaseStatus.Open);

        builder.Property(c => c.Notes)
            .HasMaxLength(2000);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(c => c.Patient)
            .WithMany(p => p.Cases)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.CaseType)
            .WithMany(ct => ct.Cases)
            .HasForeignKey(c => c.CaseTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.StatusHistory)
            .WithOne(sh => sh.Case)
            .HasForeignKey(sh => sh.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.CareTeam)
            .WithOne(ctm => ctm.Case)
            .HasForeignKey(ctm => ctm.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Notes_Collection)
            .WithOne(cn => cn.Case)
            .HasForeignKey(cn => cn.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.CaseTags)
            .WithOne(cct => cct.Case)
            .HasForeignKey(cct => cct.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
