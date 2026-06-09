using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Commands;

public record InternalMarkEntry(
    Guid SubjectId,
    Guid StudentId,
    decimal Marks,
    decimal MaxMarks);

public record EnterInternalMarksCommand(
    Guid TenantId,
    Guid SemesterId,
    Guid EnteredBy,
    IReadOnlyList<InternalMarkEntry> Marks) : IRequest<Result<int>>;

public class EnterInternalMarksHandler : IRequestHandler<EnterInternalMarksCommand, Result<int>>
{
    private readonly IExamsDbContext _db;

    public EnterInternalMarksHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<int>> Handle(EnterInternalMarksCommand request, CancellationToken cancellationToken)
    {
        int upsertCount = 0;

        foreach (var entry in request.Marks)
        {
            var existing = await _db.InternalMarks
                .FirstOrDefaultAsync(m =>
                    m.SubjectId == entry.SubjectId &&
                    m.StudentId == entry.StudentId &&
                    m.SemesterId == request.SemesterId,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Marks = entry.Marks;
                existing.MaxMarks = entry.MaxMarks;
                existing.EnteredBy = request.EnteredBy;
            }
            else
            {
                _db.InternalMarks.Add(new InternalMark
                {
                    TenantId = request.TenantId,
                    SubjectId = entry.SubjectId,
                    StudentId = entry.StudentId,
                    SemesterId = request.SemesterId,
                    Marks = entry.Marks,
                    MaxMarks = entry.MaxMarks,
                    EnteredBy = request.EnteredBy
                });
            }

            upsertCount++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(upsertCount);
    }
}
