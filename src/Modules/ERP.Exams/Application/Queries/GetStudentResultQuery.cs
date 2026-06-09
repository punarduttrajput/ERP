using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Exams.Application.Queries;

public record StudentResultDto(
    Guid SubjectId,
    string SubjectName,
    decimal InternalMarks,
    decimal ExternalMarks,
    decimal TotalMarks,
    decimal MaxMarks,
    string GradeLetter,
    decimal GradePoints,
    int Credits,
    string Status,
    decimal? GPA,
    decimal? CGPA);

public record GetStudentResultQuery(Guid StudentId, Guid SemesterId) : IRequest<Result<IReadOnlyList<StudentResultDto>>>;

public class GetStudentResultHandler : IRequestHandler<GetStudentResultQuery, Result<IReadOnlyList<StudentResultDto>>>
{
    private readonly IExamsDbContext _db;

    public GetStudentResultHandler(IExamsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<StudentResultDto>>> Handle(GetStudentResultQuery request, CancellationToken cancellationToken)
    {
        var results = await _db.StudentResults
            .Where(r =>
                r.StudentId == request.StudentId &&
                r.SemesterId == request.SemesterId &&
                r.IsPublished)
            .OrderBy(r => r.SubjectName)
            .Select(r => new StudentResultDto(
                r.SubjectId,
                r.SubjectName,
                r.InternalMarks,
                r.ExternalMarks,
                r.TotalMarks,
                r.MaxMarks,
                r.GradeLetter,
                r.GradePoints,
                r.Credits,
                r.Status.ToString(),
                r.GPA,
                r.CGPA))
            .ToListAsync(cancellationToken);

        if (results.Count == 0)
            return Result<IReadOnlyList<StudentResultDto>>.Failure("No published results found for this student and semester.");

        return Result<IReadOnlyList<StudentResultDto>>.Success(results);
    }
}
