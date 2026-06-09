using ERP.Exams.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Services.IntentHandlers;

public sealed class ExamScheduleIntentHandler
{
    private readonly IExamsDbContext _examsDb;

    public ExamScheduleIntentHandler(IExamsDbContext examsDb) => _examsDb = examsDb;

    public async Task<string> HandleAsync(Guid tenantId, Guid userId, string userMessage, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = await _examsDb.ExamSchedules
            .Where(e => e.ExamDate >= today)
            .OrderBy(e => e.ExamDate)
            .Take(5)
            .ToListAsync(ct);

        if (!upcoming.Any())
            return "No upcoming exams are scheduled at this time.";

        var lines = upcoming.Select(e =>
            $"{e.SubjectName} on {e.ExamDate:dd MMM yyyy} at {e.StartTime:hh\\:mm tt} in {e.Venue}");

        return "Your upcoming exams: " + string.Join("; ", lines) + ".";
    }
}
