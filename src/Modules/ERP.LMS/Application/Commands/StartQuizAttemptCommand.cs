using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Commands;

public record StartQuizAttemptCommand(
    Guid TenantId,
    Guid QuizId,
    Guid StudentId) : IRequest<Result<Guid>>;

public class StartQuizAttemptHandler : IRequestHandler<StartQuizAttemptCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;

    public StartQuizAttemptHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(StartQuizAttemptCommand cmd, CancellationToken ct)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == cmd.QuizId, ct);
        if (quiz is null)
            return Result.Failure<Guid>("Quiz not found.");

        var completedAttempts = await _db.QuizAttempts
            .CountAsync(a => a.QuizId == cmd.QuizId && a.StudentId == cmd.StudentId && a.IsCompleted, ct);

        if (completedAttempts >= quiz.MaxAttempts)
            return Result.Failure<Guid>("Maximum attempts reached.");

        var attempt = new QuizAttempt
        {
            TenantId  = cmd.TenantId,
            QuizId    = cmd.QuizId,
            StudentId = cmd.StudentId,
            StartedAt = DateTime.UtcNow
        };

        _db.QuizAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);
        return Result.Success(attempt.Id);
    }
}
