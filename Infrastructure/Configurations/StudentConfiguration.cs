using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.StudentId);

        builder.Property(s => s.Gender)
            .HasConversion<string>();

        builder.Property(s => s.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.PreferredArea)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.NationalId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.FacultyName)
            .HasMaxLength(100);

        builder.Property(s => s.UniversityName)
            .HasMaxLength(100);

        builder.Property(s => s.UniversityEmail)
            .HasMaxLength(150);

        builder.Property(s => s.UniversityIdCardPath)
            .HasMaxLength(500);

        builder.Property(s => s.UniversityVerificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(UniversityVerificationStatus.NotSubmitted);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
