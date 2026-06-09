using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentId,
    string BlobUrl,
    string FileName,
    DateTime SubmittedAt,
    SubmissionStatus Status,
    decimal? MarksAwarded,
    string? FacultyFeedback,
    Guid? GradedBy,
    DateTime? GradedAt);

public record GetSubmissionsQuery(Guid AssignmentId) : IRequest<Result<IReadOnlyList<SubmissionDto>>>;

public class GetSubmissionsHandler : IRequestHandler<GetSubmissionsQuery, Result<IReadOnlyList<SubmissionDto>>>
{
    private readonly ILmsDbContext _db;

    public GetSubmissionsHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<SubmissionDto>>> Handle(GetSubmissionsQuery query, CancellationToken ct)
    {
        var items = await _db.AssignmentSubmissions
            .Where(s => s.AssignmentId == query.AssignmentId && !s.IsDeleted)
            .OrderBy(s => s.SubmittedAt)
            .Select(s => new SubmissionDto(s.Id, s.AssignmentId, s.StudentId, s.BlobUrl, s.FileName, s.SubmittedAt, s.Status, s.MarksAwarded, s.FacultyFeedback, s.GradedBy, s.GradedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<SubmissionDto>>(items);
    }
}
