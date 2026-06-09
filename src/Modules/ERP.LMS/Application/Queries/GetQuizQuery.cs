using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record QuizQuestionResponseDto(
    Guid Id,
    string QuestionText,
    QuestionType QuestionType,
    string? OptionsJson,
    // Null when caller is a Student — hidden to prevent answer leakage
    string? CorrectAnswer,
    decimal Marks,
    int OrderIndex);

public record QuizDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Instructions,
    int DurationMinutes,
    int MaxAttempts,
    bool IsVisible,
    Guid CreatedBy,
    IReadOnlyList<QuizQuestionResponseDto> Questions);

public record GetQuizQuery(Guid QuizId) : IRequest<Result<QuizDto>>;

public class GetQuizHandler : IRequestHandler<GetQuizQuery, Result<QuizDto>>
{
    private readonly ILmsDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetQuizHandler(ILmsDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<Result<QuizDto>> Handle(GetQuizQuery query, CancellationToken ct)
    {
        var quiz = await _db.Quizzes
            .Include(q => q.Questions.Where(q => !q.IsDeleted))
            .FirstOrDefaultAsync(q => q.Id == query.QuizId && !q.IsDeleted, ct);

        if (quiz is null)
            return Result.Failure<QuizDto>("Quiz not found.");

        // Students must not see correct answers — prevents trivial cheating via API
        var canSeeAnswers = _currentUser.HasPermission("quizzes:answers");

        var questions = quiz.Questions
            .OrderBy(q => q.OrderIndex)
            .Select(q => new QuizQuestionResponseDto(
                q.Id,
                q.QuestionText,
                q.QuestionType,
                q.Options,
                canSeeAnswers ? q.CorrectAnswer : null,
                q.Marks,
                q.OrderIndex))
            .ToList();

        return Result.Success(new QuizDto(quiz.Id, quiz.SubjectId, quiz.BatchId, quiz.Title, quiz.Instructions, quiz.DurationMinutes, quiz.MaxAttempts, quiz.IsVisible, quiz.QuizCreatedBy, questions));
    }
}
