using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.MessageId);

        builder.Property(m => m.SenderId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(m => m.Content)
            .HasMaxLength(5000)
            .IsRequired();

        builder.Property(m => m.SentAt)
            .IsRequired();

        builder.HasIndex(m => new { m.ConversationId, m.SentAt })
            .IsDescending(false, true);
    }
}
