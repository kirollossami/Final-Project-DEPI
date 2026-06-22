using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.ConversationId);

        builder.Property(c => c.StudentUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(c => c.LandLordUserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.HasIndex(c => c.BookingId)
            .IsUnique();

        builder.HasOne(c => c.Booking)
            .WithMany()
            .HasForeignKey(c => c.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
