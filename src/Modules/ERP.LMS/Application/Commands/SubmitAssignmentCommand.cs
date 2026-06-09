using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Commands;

public record SubmitAssignmentCommand(
    Guid TenantId,
    Guid AssignmentId,
    Guid StudentId,
    byte[] FileBytes,
    string FileName) : IRequest<Result<Guid>>;

public class SubmitAssignmentHandler : IRequestHandler<SubmitAssignmentCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;
    private readonly IAzureBlobService _blob;

    public SubmitAssignmentHandler(ILmsDbContext db, IAzureBlobService blob)
    {
        _db = db;
        _blob = blob;
    }

    public async Task<Result<Guid>> Handle(SubmitAssignmentCommand cmd, CancellationToken ct)
    {
        var assignment = await _db.Assignments.FirstOrDefaultAsync(a => a.Id == cmd.AssignmentId, ct);
        if (assignment is null)
            return Result.Failure<Guid>("Assignment not found.");

        var existing = await _db.AssignmentSubmissions
            .FirstOrDefaultAsync(s => s.AssignmentId == cmd.AssignmentId && s.StudentId == cmd.StudentId, ct);
        if (existing is not null)
            return Result.Failure<Guid>("You have already submitted this assignment.");

        var blobName = $"{cmd.TenantId}/submissions/{cmd.AssignmentId}/{cmd.StudentId}/{Guid.NewGuid()}/{cmd.FileName}";
        var blobUrl = await _blob.UploadAsync("lms-content", blobName, cmd.FileBytes, "application/octet-stream", ct);

        var now = DateTime.UtcNow;
        var status = now > assignment.DueDate ? SubmissionStatus.Late : SubmissionStatus.Submitted;

        var submission = new AssignmentSubmission
        {
            TenantId     = cmd.TenantId,
            AssignmentId = cmd.AssignmentId,
            StudentId    = cmd.StudentId,
            BlobUrl      = blobUrl,
            FileName     = cmd.FileName,
            SubmittedAt  = now,
            Status       = status
        };

        _db.AssignmentSubmissions.Add(submission);

        var progress = await GetOrCreateProgress(cmd.TenantId, cmd.StudentId, assignment.SubjectId, assignment.BatchId, ct);
        progress.AssignmentsSubmitted++;
        progress.LastActivityAt = now;

        await _db.SaveChangesAsync(ct);
        return Result.Success(submission.Id);
    }

    private async Task<StudentProgress> GetOrCreateProgress(Guid tenantId, Guid studentId, Guid subjectId, Guid batchId, CancellationToken ct)
    {
        var progress = await _db.StudentProgresses
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.StudentId == studentId && p.SubjectId == subjectId && p.BatchId == batchId, ct);

        if (progress is null)
        {
            progress = new StudentProgress { TenantId = tenantId, StudentId = studentId, SubjectId = subjectId, BatchId = batchId };
            _db.StudentProgresses.Add(progress);
        }

        return progress;
    }
}
