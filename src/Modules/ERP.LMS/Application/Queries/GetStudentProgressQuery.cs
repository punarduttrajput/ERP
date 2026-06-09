using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record StudentProgressDto(
    Guid StudentId,
    Guid SubjectId,
    Guid BatchId,
    int ContentViewedCount,
    int TotalContentCount,
    double ContentViewedPct,
    int AssignmentsSubmitted,
    int TotalAssignments,
    double AssignmentSubmittedPct,
    int QuizzesTaken,
    int TotalQuizzes,
    decimal AverageQuizScore,
    DateTime? LastActivityAt);

public record GetStudentProgressQuery(Guid StudentId, Guid SubjectId, Guid BatchId) : IRequest<Result<StudentProgressDto>>;

public class GetStudentProgressHandler : IRequestHandler<GetStudentProgressQuery, Result<StudentProgressDto>>
{
    private readonly ILmsDbContext _db;

    public GetStudentProgressHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<StudentProgressDto>> Handle(GetStudentProgressQuery query, CancellationToken ct)
    {
        var p = await _db.StudentProgresses
            .FirstOrDefaultAsync(x => x.StudentId == query.StudentId && x.SubjectId == query.SubjectId && x.BatchId == query.BatchId && !x.IsDeleted, ct);

        if (p is null)
        {
            return Result.Success(new StudentProgressDto(
                query.StudentId, query.SubjectId, query.BatchId,
                0, 0, 0, 0, 0, 0, 0, 0, 0, null));
        }

        var contentPct  = p.TotalContentCount  > 0 ? p.ContentViewedCount  * 100.0 / p.TotalContentCount  : 0;
        var assignPct   = p.TotalAssignments   > 0 ? p.AssignmentsSubmitted * 100.0 / p.TotalAssignments   : 0;

        return Result.Success(new StudentProgressDto(
            p.StudentId, p.SubjectId, p.BatchId,
            p.ContentViewedCount, p.TotalContentCount, contentPct,
            p.AssignmentsSubmitted, p.TotalAssignments, assignPct,
            p.QuizzesTaken, p.TotalQuizzes, p.AverageQuizScore,
            p.LastActivityAt));
    }
}
