using ERP.Chatbot.Application.Services.IntentHandlers;
using ERP.Chatbot.Domain;

namespace ERP.Chatbot.Application.Services;

public sealed class RuleBasedIntentRouter : ILlmService
{
    private readonly FeeBalanceIntentHandler _feeBalance;
    private readonly ExamScheduleIntentHandler _examSchedule;
    private readonly TimetableIntentHandler _timetable;
    private readonly AttendanceSummaryIntentHandler _attendance;
    private readonly FacultyBriefIntentHandler _facultyBrief;
    private readonly KpiQueryIntentHandler _kpiQuery;

    public RuleBasedIntentRouter(
        FeeBalanceIntentHandler feeBalance,
        ExamScheduleIntentHandler examSchedule,
        TimetableIntentHandler timetable,
        AttendanceSummaryIntentHandler attendance,
        FacultyBriefIntentHandler facultyBrief,
        KpiQueryIntentHandler kpiQuery)
    {
        _feeBalance = feeBalance;
        _examSchedule = examSchedule;
        _timetable = timetable;
        _attendance = attendance;
        _facultyBrief = facultyBrief;
        _kpiQuery = kpiQuery;
    }

    public Task<ChatIntent> DetectIntentAsync(string userMessage, CancellationToken cancellationToken = default)
    {
        var msg = userMessage.ToLowerInvariant();

        if (msg.Contains("fee") || msg.Contains("dues") || msg.Contains("payment") || msg.Contains("balance"))
            return Task.FromResult(ChatIntent.FeeBalance);

        if (msg.Contains("exam") || msg.Contains("schedule") || msg.Contains("hall ticket") || msg.Contains("result"))
            return Task.FromResult(ChatIntent.ExamSchedule);

        if (msg.Contains("timetable") || msg.Contains("class") || msg.Contains("lecture") || msg.Contains("period"))
            return Task.FromResult(ChatIntent.Timetable);

        if (msg.Contains("attendance") || msg.Contains("present") || msg.Contains("absent"))
            return Task.FromResult(ChatIntent.AttendanceSummary);

        if (msg.Contains("assignment") || msg.Contains("submission") || msg.Contains("homework"))
            return Task.FromResult(ChatIntent.AssignmentStatus);

        if (msg.Contains("brief") || msg.Contains("pending") || msg.Contains("unmarked") || msg.Contains("ungraded"))
            return Task.FromResult(ChatIntent.FacultyDailyBrief);

        if (msg.Contains("how many") || msg.Contains("total") || msg.Contains("count") || msg.Contains("kpi"))
            return Task.FromResult(ChatIntent.KpiQuery);

        if (msg.Contains("hello") || msg.Contains("hi") || msg.Contains("hey"))
            return Task.FromResult(ChatIntent.Greeting);

        if (msg.Contains("help") || msg.Contains("what can you"))
            return Task.FromResult(ChatIntent.Help);

        return Task.FromResult(ChatIntent.Unknown);
    }

    public async Task<string> GenerateResponseAsync(
        string systemPrompt,
        IReadOnlyList<(string role, string content)> conversationHistory,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var intent = await DetectIntentAsync(userMessage, cancellationToken);

        // tenantId and userId are not available here — callers resolve via intent handlers directly.
        // This overload exists so external LLM implementations can use conversation context.
        return intent switch
        {
            ChatIntent.Greeting => "Hello! I'm your ERP assistant. I can help with fee balances, exam schedules, timetables, attendance, and more. What would you like to know?",
            ChatIntent.Help => "I can answer questions about: fee balance and dues, exam schedule and hall tickets, your timetable, attendance summary, assignment submissions, faculty daily briefing, and institution KPIs.",
            _ => "I'm not sure I understand that. Try asking about your fee balance, exam schedule, attendance, or timetable."
        };
    }
}
