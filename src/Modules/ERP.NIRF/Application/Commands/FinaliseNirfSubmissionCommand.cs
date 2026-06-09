using ERP.NIRF.Domain;
using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.NIRF.Application.Commands;

public record FinaliseNirfSubmissionCommand(Guid TenantId, Guid SubmissionId) : IRequest<Result>;

public class FinaliseNirfSubmissionHandler : IRequestHandler<FinaliseNirfSubmissionCommand, Result>
{
    private readonly INirfDbContext _db;

    public FinaliseNirfSubmissionHandler(INirfDbContext db) => _db = db;

    public async Task<Result> Handle(FinaliseNirfSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = _db.NirfSubmissions
            .FirstOrDefault(s => s.Id == request.SubmissionId && s.TenantId == request.TenantId && !s.IsDeleted);

        if (submission is null)
            return Result.Failure("Submission not found.");

        var scores = _db.NirfParameterScores
            .Where(p => p.TenantId == request.TenantId && p.SubmissionId == request.SubmissionId && !p.IsDeleted)
            .Select(p => p.Parameter)
            .ToList();

        if (!NirfParameter.All.All(p => scores.Contains(p)))
            return Result.Failure("All 5 NIRF parameter scores must be compiled before finalising.");

        submission.Status = SubmissionStatus.Reviewed;
        submission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
