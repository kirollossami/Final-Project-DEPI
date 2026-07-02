using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.HasKey(c => c.ContractId);

        builder.Property(c => c.FinalPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.DurationType)
            .HasConversion<string>();

        builder.Property(c => c.ContractNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.OwnerFullName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.OwnerNationalId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.StudentFullName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(c => c.StudentNationalId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.GeneratedPdfUrl)
            .HasMaxLength(2000);

        builder.Property(c => c.StudentSignedPdfUrl)
            .HasMaxLength(2000);

        builder.Property(c => c.OwnerSignedPdfUrl)
            .HasMaxLength(2000);

        builder.Property(c => c.FinalSignedPdfUrl)
            .HasMaxLength(2000);

        builder.Property(c => c.AdminUserId)
            .HasMaxLength(500);

        builder.Property(c => c.AdminNotes)
            .HasMaxLength(1000);

        builder.HasOne(c => c.Booking)
            .WithOne(b => b.Contract)
            .HasForeignKey<Contract>(c => c.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
