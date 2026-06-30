using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CaseNoteConfiguration : IEntityTypeConfiguration<CaseNote>
{
    public void Configure(EntityTypeBuilder<CaseNote> builder)
    {
        builder.HasKey(cn => cn.Id);
        builder.Property(cn => cn.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()")
            .ValueGeneratedOnAdd();

        builder.Property(cn => cn.Content)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(cn => cn.IsEditable)
            .HasDefaultValue(true);

        builder.Property(cn => cn.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(cn => cn.Case)
            .WithMany(c => c.Notes_Collection)
            .HasForeignKey(cn => cn.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cn => cn.CaseId);

        // No FK to User — user deletion doesn't affect notes
    }
}
