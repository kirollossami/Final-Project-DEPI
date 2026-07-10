using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class EscrowTransactionConfiguration : IEntityTypeConfiguration<EscrowTransaction>
{
    public void Configure(EntityTypeBuilder<EscrowTransaction> builder)
    {
        builder.HasKey(e => e.EscrowId);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>();

        builder.Property(e => e.TransactionType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.PaymentReference)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.ReleasedByUserId)
            .HasMaxLength(500);

        builder.Property(e => e.ReleaseTransactionId)
            .HasMaxLength(500);

        builder.Property(e => e.ReleaseNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.RefundTransactionId)
            .HasMaxLength(500);

        builder.Property(e => e.RefundReason)
            .HasMaxLength(1000);

        builder.Property(e => e.LandlordPayoutTransactionId)
            .HasMaxLength(500);

        builder.Property(e => e.LandlordPayoutAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.PlatformFee)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.PlatformFeePercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.HasOne(e => e.Booking)
            .WithMany()
            .HasForeignKey(e => e.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Payment)
            .WithMany()
            .HasForeignKey(e => e.PaymentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.ContractId)
            .IsRequired(false);

        builder.HasOne(e => e.Contract)
            .WithMany(c => c.EscrowTransactions)
            .HasForeignKey(e => e.ContractId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
