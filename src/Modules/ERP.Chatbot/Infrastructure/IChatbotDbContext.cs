using ERP.Chatbot.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Infrastructure;

public interface IChatbotDbContext
{
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
