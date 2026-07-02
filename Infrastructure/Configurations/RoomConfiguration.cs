using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.RoomId);

        builder.Property(r => r.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.PricePerMonth)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(r => r.Capacity)
            .IsRequired();

        builder.Property(r => r.CurrentOccupancy)
            .HasDefaultValue(0);

        builder.Property(r => r.RoomType)
            .HasConversion<string>();

        builder.HasOne(r => r.HousingUnit)
            .WithMany(h => h.Rooms)
            .HasForeignKey(r => r.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Beds)
            .WithOne(b => b.Room)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
