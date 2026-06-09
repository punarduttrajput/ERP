using ERP.Chatbot.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.Chatbot.Infrastructure;

public class ChatSessionConfiguration : IEntityTypeConfiguration<ChatSession>
{
    public void Configure(EntityTypeBuilder<ChatSession> builder)
    {
        builder.ToTable("chat_sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SessionKey).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.MessageCount).HasDefaultValue(0);
        builder.HasMany(x => x.Messages).WithOne(x => x.Session).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TenantId, x.UserId, x.StartedAt });
        builder.HasIndex(x => new { x.TenantId, x.SessionKey });
    }
}

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).HasColumnType("text").IsRequired();
        builder.Property(x => x.Role).HasConversion<int>();
        builder.Property(x => x.Intent).HasConversion<int>();
        builder.HasIndex(x => new { x.TenantId, x.SessionId, x.SentAt });
    }
}
