using ERP.NIRF.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NIRF.Application.Queries;

public record NirfParameterScoreDto(
    string Parameter,
    decimal RawScore,
    decimal WeightedScore,
    decimal Weight,
    string DataJson,
    bool IsManualOverride);

public record NirfSubmissionDto(
    Guid Id,
    int RankingYear,
    string Category,
    string Status,
    decimal? OverallScore,
    int? EstimatedRank,
    DateTime? SubmittedAt,
    IReadOnlyList<NirfParameterScoreDto> ParameterScores);

public record GetNirfSubmissionQuery(Guid TenantId, Guid SubmissionId) : IRequest<Result<NirfSubmissionDto>>;

public class GetNirfSubmissionHandler : IRequestHandler<GetNirfSubmissionQuery, Result<NirfSubmissionDto>>
{
    private readonly INirfDbContext _db;

    public GetNirfSubmissionHandler(INirfDbContext db) => _db = db;

    public Task<Result<NirfSubmissionDto>> Handle(GetNirfSubmissionQuery request, CancellationToken cancellationToken)
    {
        var submission = _db.NirfSubmissions
            .Include(s => s.ParameterScores.Where(p => !p.IsDeleted))
            .FirstOrDefault(s => s.Id == request.SubmissionId && s.TenantId == request.TenantId && !s.IsDeleted);

        if (submission is null)
            return Task.FromResult(Result.Failure<NirfSubmissionDto>("Submission not found."));

        var dto = new NirfSubmissionDto(
            submission.Id,
            submission.RankingYear,
            submission.Category,
            submission.Status.ToString(),
            submission.OverallScore,
            submission.EstimatedRank,
            submission.SubmittedAt,
            submission.ParameterScores.Select(p => new NirfParameterScoreDto(
                p.Parameter, p.RawScore, p.WeightedScore, p.Weight, p.DataJson, p.IsManualOverride
            )).ToList());

        return Task.FromResult(Result.Success(dto));
    }
}
