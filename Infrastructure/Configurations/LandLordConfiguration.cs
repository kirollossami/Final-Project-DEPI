using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class LandLordConfiguration : IEntityTypeConfiguration<LandLord>
{
    public void Configure(EntityTypeBuilder<LandLord> builder)
    {
        builder.HasKey(l => l.LandLordId);

        builder.Property(l => l.NationalId)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(l => l.NationalIdImageUrl)
            .HasMaxLength(500);

        builder.Property(l => l.CompanyName)
            .HasMaxLength(200);

        builder.Property(l => l.PropertyOwnerShipProof)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.HousingUnitDocumentationUrl)
            .HasMaxLength(500);

        builder.Property(l => l.VerificationStatus)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.IsVerified)
            .HasDefaultValue(false);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
