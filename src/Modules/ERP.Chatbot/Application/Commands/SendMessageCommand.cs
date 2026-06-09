using ERP.Chatbot.Application.Services;
using ERP.Chatbot.Application.Services.IntentHandlers;
using ERP.Chatbot.Domain;
using ERP.Chatbot.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ERP.Chatbot.Application.Commands;

public record SendMessageCommand(
    Guid TenantId,
    Guid UserId,
    string Message,
    string? SessionKey = null) : IRequest<Result<ChatResponseDto>>;

public record ChatResponseDto(string Response, ChatIntent Intent, string SessionKey);

public class SendMessageHandler : IRequestHandler<SendMessageCommand, Result<ChatResponseDto>>
{
    private readonly IChatbotDbContext _db;
    private readonly ILlmService _llm;
    private readonly ICacheService _cache;
    private readonly FeeBalanceIntentHandler _feeBalance;
    private readonly ExamScheduleIntentHandler _examSchedule;
    private readonly TimetableIntentHandler _timetable;
    private readonly AttendanceSummaryIntentHandler _attendance;
    private readonly FacultyBriefIntentHandler _facultyBrief;
    private readonly KpiQueryIntentHandler _kpiQuery;

    private static readonly TimeSpan RedisTtl = TimeSpan.FromHours(24);

    public SendMessageHandler(
        IChatbotDbContext db,
        ILlmService llm,
        ICacheService cache,
        FeeBalanceIntentHandler feeBalance,
        ExamScheduleIntentHandler examSchedule,
        TimetableIntentHandler timetable,
        AttendanceSummaryIntentHandler attendance,
        FacultyBriefIntentHandler facultyBrief,
        KpiQueryIntentHandler kpiQuery)
    {
        _db = db;
        _llm = llm;
        _cache = cache;
        _feeBalance = feeBalance;
        _examSchedule = examSchedule;
        _timetable = timetable;
        _attendance = attendance;
        _facultyBrief = facultyBrief;
        _kpiQuery = kpiQuery;
    }

    public async Task<Result<ChatResponseDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var sessionKey = request.SessionKey
            ?? $"chat:{request.TenantId}:{request.UserId}:{DateTime.UtcNow:yyyyMMdd}";

        var redisKey = $"chat_session:{request.TenantId}:{request.UserId}:{DateTime.UtcNow:yyyyMMdd}";

        var session = await GetOrCreateSessionAsync(request, sessionKey, cancellationToken);

        var intent = await _llm.DetectIntentAsync(request.Message, cancellationToken);

        var responseText = await RouteToHandlerAsync(intent, request.TenantId, request.UserId, request.Message, cancellationToken);

        var cachedMessages = await _cache.GetAsync<List<CachedMessage>>(redisKey, cancellationToken) ?? new List<CachedMessage>();
        cachedMessages.Add(new CachedMessage(MessageRole.User, request.Message, DateTime.UtcNow));
        cachedMessages.Add(new CachedMessage(MessageRole.Assistant, responseText, DateTime.UtcNow));
        await _cache.SetAsync(redisKey, cachedMessages, RedisTtl, cancellationToken);

        // Fire-and-forget DB persistence: wraps in try-catch so a DB failure never loses the response.
        _ = Task.Run(async () =>
        {
            try
            {
                var userMsg = new ChatMessage
                {
                    SessionId = session.Id,
                    TenantId = request.TenantId,
                    Role = MessageRole.User,
                    Content = request.Message,
                    Intent = intent,
                    SentAt = DateTime.UtcNow
                };
                var assistantMsg = new ChatMessage
                {
                    SessionId = session.Id,
                    TenantId = request.TenantId,
                    Role = MessageRole.Assistant,
                    Content = responseText,
                    Intent = intent,
                    SentAt = DateTime.UtcNow
                };
                _db.ChatMessages.Add(userMsg);
                _db.ChatMessages.Add(assistantMsg);

                session.LastMessageAt = DateTime.UtcNow;
                session.MessageCount += 2;

                await _db.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // Intentionally swallowed — DB persistence is best-effort; Redis is authoritative for hot sessions.
            }
        });

        return Result<ChatResponseDto>.Success(new ChatResponseDto(responseText, intent, sessionKey));
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(SendMessageCommand request, string sessionKey, CancellationToken ct)
    {
        var session = await _db.ChatSessions
            .FirstOrDefaultAsync(s => s.TenantId == request.TenantId && s.UserId == request.UserId && s.SessionKey == sessionKey && !s.IsDeleted, ct);

        if (session is not null)
            return session;

        session = new ChatSession
        {
            TenantId = request.TenantId,
            UserId = request.UserId,
            SessionKey = sessionKey,
            StartedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    private async Task<string> RouteToHandlerAsync(ChatIntent intent, Guid tenantId, Guid userId, string message, CancellationToken ct)
    {
        return intent switch
        {
            ChatIntent.FeeBalance => await _feeBalance.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.ExamSchedule => await _examSchedule.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.Timetable => await _timetable.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.AttendanceSummary => await _attendance.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.AssignmentStatus => await _attendance.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.FacultyDailyBrief => await _facultyBrief.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.KpiQuery => await _kpiQuery.HandleAsync(tenantId, userId, message, ct),
            ChatIntent.Greeting => Task.FromResult("Hello! I'm your ERP assistant. I can help with fee balances, exam schedules, timetables, attendance, and more. What would you like to know?").Result,
            ChatIntent.Help => "I can answer questions about: fee balance and dues, exam schedule and hall tickets, your timetable, attendance summary, assignment submissions, faculty daily briefing, and institution KPIs.",
            _ => "I'm not sure I understand that. Try asking about your fee balance, exam schedule, attendance, or timetable."
        };
    }
}

public record CachedMessage(MessageRole Role, string Content, DateTime SentAt);
