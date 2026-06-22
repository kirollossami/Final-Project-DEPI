using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class UnitImageConfiguration : IEntityTypeConfiguration<UnitImage>
{
    public void Configure(EntityTypeBuilder<UnitImage> builder)
    {
        builder.HasKey(ui => ui.UnitImageId);

        builder.Property(ui => ui.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ui => ui.Description)
            .HasMaxLength(500);

        builder.Property(ui => ui.DisplayOrder)
            .HasDefaultValue(0);

        builder.HasOne(ui => ui.HousingUnit)
            .WithMany(h => h.UnitImages)
            .HasForeignKey(ui => ui.HousingUnitId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
