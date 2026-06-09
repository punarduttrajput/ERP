using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.LMS.Application.Commands;

public record QuizQuestionDto(
    string QuestionText,
    QuestionType QuestionType,
    string? OptionsJson,
    string? CorrectAnswer,
    decimal Marks,
    int OrderIndex);

public record CreateQuizCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Instructions,
    int DurationMinutes,
    int MaxAttempts,
    Guid CreatedBy,
    IReadOnlyList<QuizQuestionDto> Questions) : IRequest<Result<Guid>>;

public class CreateQuizHandler : IRequestHandler<CreateQuizCommand, Result<Guid>>
{
    private readonly ILmsDbContext _db;

    public CreateQuizHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateQuizCommand cmd, CancellationToken ct)
    {
        var quiz = new Quiz
        {
            TenantId        = cmd.TenantId,
            SubjectId       = cmd.SubjectId,
            BatchId         = cmd.BatchId,
            Title           = cmd.Title,
            Instructions    = cmd.Instructions,
            DurationMinutes = cmd.DurationMinutes,
            MaxAttempts     = cmd.MaxAttempts,
            IsVisible       = true,
            QuizCreatedBy   = cmd.CreatedBy
        };

        foreach (var q in cmd.Questions)
        {
            quiz.Questions.Add(new QuizQuestion
            {
                TenantId     = cmd.TenantId,
                QuizId       = quiz.Id,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Options      = q.OptionsJson,
                CorrectAnswer = q.CorrectAnswer,
                Marks        = q.Marks,
                OrderIndex   = q.OrderIndex
            });
        }

        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync(ct);
        return Result.Success(quiz.Id);
    }
}
