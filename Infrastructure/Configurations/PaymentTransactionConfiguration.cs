using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(pt => pt.TransactionId);

        builder.Property(pt => pt.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(pt => pt.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pt => pt.GatewayStatus)
            .HasConversion<string>();

        builder.Property(pt => pt.PaymobOrderId)
            .HasMaxLength(500);

        builder.Property(pt => pt.PaymobIntentionId)
            .HasMaxLength(500);

        builder.Property(pt => pt.PaymobTransactionId)
            .HasMaxLength(500);

        builder.Property(pt => pt.PaymentToken)
            .HasMaxLength(1000);

        builder.Property(pt => pt.PaymentUrl)
            .HasMaxLength(2000);

        builder.Property(pt => pt.RawResponse)
            .HasMaxLength(5000);

        builder.Property(pt => pt.CallbackSuccess)
            .HasMaxLength(500);

        builder.Property(pt => pt.CallbackPending)
            .HasMaxLength(500);

        builder.Property(pt => pt.CallbackFailed)
            .HasMaxLength(500);

        builder.HasOne(pt => pt.Payment)
            .WithMany(p => p.PaymentTransactions)
            .HasForeignKey(pt => pt.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
