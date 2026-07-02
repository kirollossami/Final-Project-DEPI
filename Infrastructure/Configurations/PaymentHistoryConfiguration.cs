using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework configuration for PaymentHistory
/// </summary>
public class PaymentHistoryConfiguration : IEntityTypeConfiguration<PaymentHistory>
{
    public void Configure(EntityTypeBuilder<PaymentHistory> builder)
    {
        builder.HasKey(ph => ph.HistoryId);

        builder.Property(ph => ph.HistoryId)
            .ValueGeneratedOnAdd();

        builder.Property(ph => ph.UserId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ph => ph.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ph => ph.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ph => ph.Amount)
            .HasPrecision(18, 2);

        builder.Property(ph => ph.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(ph => ph.PreviousStatus)
            .HasMaxLength(100);

        builder.Property(ph => ph.NewStatus)
            .HasMaxLength(100);

        builder.Property(ph => ph.ActorUserId)
            .HasMaxLength(256);

        builder.Property(ph => ph.ActorRole)
            .HasMaxLength(50);

        builder.Property(ph => ph.IpAddress)
            .HasMaxLength(50);

        builder.Property(ph => ph.CreatedAt)
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for quick queries
        builder.HasIndex(ph => ph.PaymentId);
        builder.HasIndex(ph => ph.BookingId);
        builder.HasIndex(ph => ph.UserId);
        builder.HasIndex(ph => ph.CreatedAt);
        builder.HasIndex(ph => new { ph.UserId, ph.CreatedAt });
        builder.HasIndex(ph => new { ph.BookingId, ph.CreatedAt });

        // Foreign keys
        builder.HasOne(ph => ph.Payment)
            .WithMany()
            .HasForeignKey(ph => ph.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ph => ph.Booking)
            .WithMany()
            .HasForeignKey(ph => ph.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ph => ph.EscrowTransaction)
            .WithMany()
            .HasForeignKey(ph => ph.EscrowTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(ph => ph.User)
            .WithMany()
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
