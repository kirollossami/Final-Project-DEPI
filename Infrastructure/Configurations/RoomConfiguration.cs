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

        builder.Property(r => r.RoomType)
            .HasConversion<string>();

        builder.HasOne(r => r.HousingUnit)
            .WithMany(h => h.Rooms)
            .HasForeignKey(r => r.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
