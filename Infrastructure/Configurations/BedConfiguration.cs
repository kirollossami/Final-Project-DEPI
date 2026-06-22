using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.HasKey(b => b.BedId);

        builder.Property(b => b.BedNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.IsOccupied)
            .HasDefaultValue(false);

        builder.HasOne(b => b.Room)
            .WithMany(r => r.Beds)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.Bookings)
            .WithOne(bk => bk.Bed)
            .HasForeignKey(bk => bk.BedId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
