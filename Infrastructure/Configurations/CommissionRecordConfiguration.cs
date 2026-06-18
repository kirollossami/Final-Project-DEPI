using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class CommissionRecordConfiguration : IEntityTypeConfiguration<CommissionRecord>
{
    public void Configure(EntityTypeBuilder<CommissionRecord> builder)
    {
        builder.HasKey(cr => cr.CommissionRecordId);

        builder.Property(cr => cr.Rate)
            .HasPrecision(5, 4);

        builder.Property(cr => cr.Amount)
            .HasPrecision(18, 2);

        builder.HasOne(cr => cr.Booking)
            .WithOne(b => b.CommissionRecord)
            .HasForeignKey<CommissionRecord>(cr => cr.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
