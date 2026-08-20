using Harness.Modules.Customer.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Harness.Modules.Customer.Infrastructure.Persistence;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions", "customer");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(20);
        builder.Property(x => x.AssignedAgentName).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasMany(x => x.Messages)
               .WithOne()
               .HasForeignKey(x => x.ChatSessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages", "customer");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SenderId).HasMaxLength(100);
        builder.Property(x => x.MessageText).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.SenderType).HasConversion<int>().IsRequired();
    }
}
