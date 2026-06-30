using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class CareTeamMemberConfiguration : IEntityTypeConfiguration<CareTeamMember>
{
    public void Configure(EntityTypeBuilder<CareTeamMember> builder)
    {
        builder.HasKey(ctm => ctm.Id);
        builder.Property(ctm => ctm.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()")
            .ValueGeneratedOnAdd();

        builder.Property(ctm => ctm.TeamRole)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ctm => ctm.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(ctm => ctm.Case)
            .WithMany(c => c.CareTeam)
            .HasForeignKey(ctm => ctm.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ctm => ctm.User)
            .WithMany()
            .HasForeignKey(ctm => ctm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
