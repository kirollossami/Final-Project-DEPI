using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.HasKey(pr => pr.ReceiptId);

        builder.Property(pr => pr.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pr => pr.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pr => pr.Type)
            .HasConversion<string>();

        builder.Property(pr => pr.ReceiptNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pr => pr.IssuedToUserId)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pr => pr.IssuedToRole)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(pr => pr.IssuedToName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pr => pr.TransactionReference)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pr => pr.PaymentMethod)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pr => pr.ReceiptData)
            .HasMaxLength(5000);

        builder.Property(pr => pr.ReceiptPdfUrl)
            .HasMaxLength(2000);

        // Always write CreatedAt from application — use HasDefaultValueSql as a safety net
        builder.Property(pr => pr.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(pr => pr.Payment)
            .WithMany(p => p.PaymentReceipts)
            .HasForeignKey(pr => pr.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        // EscrowId is optional for PaymentReceived receipts (no escrow exists yet).
        // Make the FK nullable so Guid.Empty / null receipts don't cause FK violations.
        builder.Property(pr => pr.EscrowId)
            .IsRequired(false);

        builder.HasOne(pr => pr.EscrowTransaction)
            .WithMany(e => e.PaymentReceipts)
            .HasForeignKey(pr => pr.EscrowId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
