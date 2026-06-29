using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HCM.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MRN)
            .IsRequired()
            .HasMaxLength(20);
        builder.HasIndex(p => p.MRN).IsUnique();

        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DateOfBirth)
            .IsRequired();

        builder.Property(p => p.Gender)
            .HasMaxLength(20);
        builder.Property(p => p.Phone)
            .HasMaxLength(20);
        builder.Property(p => p.Email)
            .HasMaxLength(256);
        builder.Property(p => p.Address)
            .HasMaxLength(500);
        builder.Property(p => p.City)
            .HasMaxLength(100);
        builder.Property(p => p.State)
            .HasMaxLength(50);
        builder.Property(p => p.ZipCode)
            .HasMaxLength(20);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(p => new { p.FirstName, p.LastName });

        builder.HasMany(p => p.Insurance)
            .WithOne(pi => pi.Patient)
            .HasForeignKey(pi => pi.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Cases)
            .WithOne(c => c.Patient)
            .HasForeignKey(c => c.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
