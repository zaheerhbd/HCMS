using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CaseStatusHistoryConfiguration : IEntityTypeConfiguration<CaseStatusHistory>
{
    public void Configure(EntityTypeBuilder<CaseStatusHistory> builder)
    {
        builder.HasKey(csh => csh.Id);
        builder.Property(csh => csh.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()")
            .ValueGeneratedOnAdd();

        builder.Property(csh => csh.FromStatus)
            .HasMaxLength(50);

        builder.Property(csh => csh.ToStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(csh => csh.Comment)
            .HasMaxLength(500);

        builder.HasOne(csh => csh.Case)
            .WithMany(c => c.StatusHistory)
            .HasForeignKey(csh => csh.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        // No FK to User — user deletion doesn't affect history
    }
}
