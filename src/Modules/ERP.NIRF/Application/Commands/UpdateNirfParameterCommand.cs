using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using System.Text.Json;

namespace ERP.NIRF.Application.Commands;

public record UpdateNirfParameterCommand(
    Guid TenantId,
    Guid SubmissionId,
    string Parameter,
    decimal RawScore,
    object? DataOverride = null) : IRequest<Result>;

public class UpdateNirfParameterHandler : IRequestHandler<UpdateNirfParameterCommand, Result>
{
    private const decimal WeightTL = 0.30m;
    private const decimal WeightR  = 0.30m;
    private const decimal WeightGO = 0.20m;
    private const decimal WeightO  = 0.10m;
    private const decimal WeightP  = 0.10m;

    private readonly INirfDbContext _db;

    public UpdateNirfParameterHandler(INirfDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateNirfParameterCommand request, CancellationToken cancellationToken)
    {
        var submission = _db.NirfSubmissions
            .FirstOrDefault(s => s.Id == request.SubmissionId && s.TenantId == request.TenantId && !s.IsDeleted);

        if (submission is null)
            return Result.Failure("Submission not found.");

        if (submission.Status == Domain.SubmissionStatus.Submitted)
            return Result.Failure("Cannot modify a submitted NIRF submission.");

        if (!new[] { Domain.NirfParameter.TeachingLearning, Domain.NirfParameter.Research,
                Domain.NirfParameter.GraduationOutcomes, Domain.NirfParameter.Outreach,
                Domain.NirfParameter.Perception }.Contains(request.Parameter))
            return Result.Failure($"Unknown parameter: {request.Parameter}");

        decimal weight = request.Parameter switch
        {
            Domain.NirfParameter.TeachingLearning    => WeightTL,
            Domain.NirfParameter.Research            => WeightR,
            Domain.NirfParameter.GraduationOutcomes  => WeightGO,
            Domain.NirfParameter.Outreach            => WeightO,
            _                                        => WeightP
        };

        var existing = _db.NirfParameterScores
            .FirstOrDefault(p => p.TenantId == request.TenantId
                && p.SubmissionId == request.SubmissionId
                && p.Parameter == request.Parameter
                && !p.IsDeleted);

        if (existing is not null)
        {
            existing.RawScore = request.RawScore;
            existing.WeightedScore = Math.Round(request.RawScore * weight, 2);
            existing.Weight = weight;
            existing.IsManualOverride = true;
            if (request.DataOverride is not null)
                existing.DataJson = JsonSerializer.Serialize(request.DataOverride);
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.NirfParameterScores.Add(new Domain.NirfParameterScore
            {
                TenantId = request.TenantId,
                SubmissionId = request.SubmissionId,
                Parameter = request.Parameter,
                RawScore = request.RawScore,
                WeightedScore = Math.Round(request.RawScore * weight, 2),
                Weight = weight,
                DataJson = request.DataOverride is not null ? JsonSerializer.Serialize(request.DataOverride) : "{}",
                IsManualOverride = true
            });
        }

        var allScores = _db.NirfParameterScores
            .Where(p => p.TenantId == request.TenantId && p.SubmissionId == request.SubmissionId && !p.IsDeleted)
            .ToList();

        submission.OverallScore = Math.Round(allScores.Sum(s => s.WeightedScore), 2);
        submission.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
