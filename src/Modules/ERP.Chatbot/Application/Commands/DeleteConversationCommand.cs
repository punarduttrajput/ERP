using ERP.Chatbot.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Commands;

public record DeleteConversationCommand(Guid TenantId, Guid UserId) : IRequest<Result>;

public class DeleteConversationHandler : IRequestHandler<DeleteConversationCommand, Result>
{
    private readonly IChatbotDbContext _db;
    private readonly ICacheService _cache;

    public DeleteConversationHandler(IChatbotDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result> Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var sessions = await _db.ChatSessions
            .Where(s => s.TenantId == request.TenantId && s.UserId == request.UserId && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsDeleted = true;
            session.DeletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Remove all Redis keys for this user across all daily sessions
        await _cache.RemoveByPatternAsync($"chat_session:{request.TenantId}:{request.UserId}:*", cancellationToken);

        // Background deletion of message content — non-blocking compliance action
        _ = Task.Run(async () =>
        {
            try
            {
                var sessionIds = sessions.Select(s => s.Id).ToList();
                var messages = await _db.ChatMessages
                    .Where(m => sessionIds.Contains(m.SessionId))
                    .ToListAsync(CancellationToken.None);

                foreach (var msg in messages)
                    msg.IsDeleted = true;

                await _db.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // Message soft-deletion is best-effort; the session IsDeleted flag is the primary DPDP signal.
            }
        });

        return Result.Success();
    }
}
