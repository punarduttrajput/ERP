using ERP.Chatbot.Application.Commands;
using ERP.Chatbot.Domain;
using ERP.Chatbot.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Queries;

public record GetConversationHistoryQuery(
    Guid TenantId,
    Guid UserId,
    string? SessionKey = null,
    int LastN = 20) : IRequest<Result<List<ChatMessageDto>>>;

public record ChatMessageDto(MessageRole Role, string Content, ChatIntent Intent, DateTime SentAt);

public class GetConversationHistoryHandler : IRequestHandler<GetConversationHistoryQuery, Result<List<ChatMessageDto>>>
{
    private readonly IChatbotDbContext _db;
    private readonly ICacheService _cache;

    public GetConversationHistoryHandler(IChatbotDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<List<ChatMessageDto>>> Handle(GetConversationHistoryQuery request, CancellationToken cancellationToken)
    {
        var sessionKey = request.SessionKey
            ?? $"chat:{request.TenantId}:{request.UserId}:{DateTime.UtcNow:yyyyMMdd}";

        var redisKey = $"chat_session:{request.TenantId}:{request.UserId}:{DateTime.UtcNow:yyyyMMdd}";

        var cached = await _cache.GetAsync<List<CachedMessage>>(redisKey, cancellationToken);
        if (cached is not null && cached.Any())
        {
            var hot = cached
                .TakeLast(request.LastN)
                .Select(m => new ChatMessageDto(m.Role, m.Content, ChatIntent.Unknown, m.SentAt))
                .ToList();
            return Result<List<ChatMessageDto>>.Success(hot);
        }

        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && s.UserId == request.UserId && s.SessionKey == sessionKey && !s.IsDeleted, cancellationToken);

        if (session is null)
            return Result<List<ChatMessageDto>>.Success(new List<ChatMessageDto>());

        var messages = await _db.ChatMessages
            .Where(m => m.SessionId == session.Id && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Take(request.LastN)
            .OrderBy(m => m.SentAt)
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.Intent, m.SentAt))
            .ToListAsync(cancellationToken);

        return Result<List<ChatMessageDto>>.Success(messages);
    }
}
