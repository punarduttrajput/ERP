using ERP.Shared.Domain;

namespace ERP.Chatbot.Domain;

public class ChatMessage : TenantEntity
{
    public Guid SessionId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public ChatIntent Intent { get; set; } = ChatIntent.Unknown;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public int? TokensUsed { get; set; }
    public ChatSession? Session { get; set; }
}
