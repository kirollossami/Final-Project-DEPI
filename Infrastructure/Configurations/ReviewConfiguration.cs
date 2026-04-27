using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.ReviewId);

        builder.Property(r => r.Rating)
            .IsRequired();

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.HasOne(r => r.HousingUnit)
            .WithMany(h => h.Reviews)
            .HasForeignKey(r => r.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Student)
            .WithMany(s => s.Reviews)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
