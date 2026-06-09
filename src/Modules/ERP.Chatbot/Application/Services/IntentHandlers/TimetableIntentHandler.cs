using ERP.Timetable.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ERP.Chatbot.Application.Services.IntentHandlers;

public sealed class TimetableIntentHandler
{
    private readonly ITimetableDbContext _timetableDb;

    public TimetableIntentHandler(ITimetableDbContext timetableDb) => _timetableDb = timetableDb;

    public async Task<string> HandleAsync(Guid tenantId, Guid userId, string userMessage, CancellationToken ct)
    {
        var todayDow = (int)DateTime.UtcNow.DayOfWeek;

        var entries = await _timetableDb.TimetableEntries
            .Include(e => e.TimeSlot)
            .Include(e => e.Room)
            .Where(e => e.TimeSlot != null && e.TimeSlot.DayOfWeek == todayDow && !e.TimeSlot.IsBreak)
            .OrderBy(e => e.TimeSlot!.PeriodNumber)
            .ToListAsync(ct);

        if (!entries.Any())
            return "No classes are scheduled for today.";

        var lines = entries.Select(e =>
            $"Period {e.TimeSlot!.PeriodNumber} at {e.TimeSlot.StartTime:hh\\:mm tt} in {e.Room?.Id}");

        return "Today's schedule: " + string.Join(", ", lines) + ".";
    }
}
