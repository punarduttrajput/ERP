using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Commands;

public record ExternalMarkEntry(
    Guid ExamScheduleId,
    Guid StudentId,
    decimal Marks,
    bool IsAbsent);

public record EnterExternalMarksCommand(
    Guid TenantId,
    Guid SemesterId,
    Guid EnteredBy,
    IReadOnlyList<ExternalMarkEntry> Marks) : IRequest<Result<int>>;

public class EnterExternalMarksHandler : IRequestHandler<EnterExternalMarksCommand, Result<int>>
{
    private readonly IExamsDbContext _db;

    public EnterExternalMarksHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<int>> Handle(EnterExternalMarksCommand request, CancellationToken cancellationToken)
    {
        int upsertCount = 0;

        foreach (var entry in request.Marks)
        {
            var existing = await _db.ExternalMarks
                .FirstOrDefaultAsync(m =>
                    m.ExamScheduleId == entry.ExamScheduleId &&
                    m.StudentId == entry.StudentId,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Marks = entry.IsAbsent ? 0m : entry.Marks;
                existing.IsAbsent = entry.IsAbsent;
                existing.EnteredBy = request.EnteredBy;
            }
            else
            {
                _db.ExternalMarks.Add(new ExternalMark
                {
                    TenantId = request.TenantId,
                    ExamScheduleId = entry.ExamScheduleId,
                    StudentId = entry.StudentId,
                    Marks = entry.IsAbsent ? 0m : entry.Marks,
                    IsAbsent = entry.IsAbsent,
                    EnteredBy = request.EnteredBy
                });
            }

            upsertCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(upsertCount);
    }
}
