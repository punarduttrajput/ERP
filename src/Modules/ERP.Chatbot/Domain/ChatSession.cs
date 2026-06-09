using ERP.Shared.Domain;

namespace ERP.Chatbot.Domain;

public class ChatSession : TenantEntity
{
    public Guid UserId { get; set; }
    public string SessionKey { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public int MessageCount { get; set; } = 0;
    public new bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
