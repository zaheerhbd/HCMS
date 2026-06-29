using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class PatientInsuranceConfiguration : IEntityTypeConfiguration<PatientInsurance>
{
    public void Configure(EntityTypeBuilder<PatientInsurance> builder)
    {
        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.InsurancePlan)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pi => pi.MemberId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pi => pi.GroupNumber)
            .HasMaxLength(100);

        builder.Property(pi => pi.SubscriberName)
            .HasMaxLength(200);

        builder.Property(pi => pi.IsPrimary)
            .HasDefaultValue(true);

        builder.HasOne(pi => pi.Patient)
            .WithMany(p => p.Insurance)
            .HasForeignKey(pi => pi.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
