using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasKey(w => w.WishlistId);

        builder.HasOne(w => w.Student)
            .WithMany(s => s.Wishlists)
            .HasForeignKey(w => w.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.HousingUnit)
            .WithMany(h => h.WishlistedBy)
            .HasForeignKey(w => w.HousingUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
