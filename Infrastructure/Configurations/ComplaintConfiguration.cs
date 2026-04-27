using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.HasKey(c => c.ComplaintId);

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.Status)
            .HasConversion<string>();

        builder.HasOne(c => c.Student)
            .WithMany(s => s.Complaints)
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.LandLord)
            .WithMany()
            .HasForeignKey(c => c.LandLordId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
