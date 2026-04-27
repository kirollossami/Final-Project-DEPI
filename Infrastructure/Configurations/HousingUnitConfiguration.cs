using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class HousingUnitConfiguration : IEntityTypeConfiguration<HousingUnit>
{
    public void Configure(EntityTypeBuilder<HousingUnit> builder)
    {
        builder.HasKey(h => h.HousingUnitId);

        builder.Property(h => h.Price)
            .HasPrecision(18, 2);

        builder.Property(h => h.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.Description)
            .HasMaxLength(2000);

        builder.Property(h => h.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(h => h.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.Area)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.Location)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(h => h.Rules)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(h => h.GenderAllowed)
            .HasConversion<string>();

        builder.Property(h => h.AverageRating)
            .HasPrecision(3, 2);

        builder.HasOne(h => h.LandLord)
            .WithMany(l => l.HousingUnits)
            .HasForeignKey(h => h.LandLordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(h => h.Rooms)
            .WithOne(r => r.HousingUnit)
            .HasForeignKey(r => r.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(h => h.Reviews)
            .WithOne(r => r.HousingUnit)
            .HasForeignKey(r => r.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(h => h.WishlistedBy)
            .WithOne(w => w.HousingUnit)
            .HasForeignKey(w => w.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
