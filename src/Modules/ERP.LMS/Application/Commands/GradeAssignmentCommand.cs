using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Commands;

public record GradeAssignmentCommand(
    Guid SubmissionId,
    decimal MarksAwarded,
    string? FacultyFeedback,
    Guid GradedBy) : IRequest<Result>;

public class GradeAssignmentHandler : IRequestHandler<GradeAssignmentCommand, Result>
{
    private readonly ILmsDbContext _db;

    public GradeAssignmentHandler(ILmsDbContext db) => _db = db;

    public async Task<Result> Handle(GradeAssignmentCommand cmd, CancellationToken ct)
    {
        var submission = await _db.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.Id == cmd.SubmissionId, ct);
        if (submission is null)
            return Result.Failure("Submission not found.");

        submission.MarksAwarded   = cmd.MarksAwarded;
        submission.FacultyFeedback = cmd.FacultyFeedback;
        submission.GradedBy       = cmd.GradedBy;
        submission.GradedAt       = DateTime.UtcNow;
        submission.Status         = SubmissionStatus.Graded;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
