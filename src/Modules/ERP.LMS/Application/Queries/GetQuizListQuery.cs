using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record QuizSummaryDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Instructions,
    int DurationMinutes,
    int MaxAttempts,
    bool IsVisible,
    Guid CreatedBy,
    DateTime CreatedAt);

public record GetQuizListQuery(Guid SubjectId, Guid BatchId) : IRequest<Result<IReadOnlyList<QuizSummaryDto>>>;

public class GetQuizListHandler : IRequestHandler<GetQuizListQuery, Result<IReadOnlyList<QuizSummaryDto>>>
{
    private readonly ILmsDbContext _db;

    public GetQuizListHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<QuizSummaryDto>>> Handle(GetQuizListQuery query, CancellationToken ct)
    {
        var items = await _db.Quizzes
            .Where(q => q.SubjectId == query.SubjectId && q.BatchId == query.BatchId && !q.IsDeleted)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new QuizSummaryDto(q.Id, q.SubjectId, q.BatchId, q.Title, q.Instructions, q.DurationMinutes, q.MaxAttempts, q.IsVisible, q.QuizCreatedBy, q.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<QuizSummaryDto>>(items);
    }
}
