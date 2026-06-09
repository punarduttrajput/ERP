using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Commands;

public record AnswerInput(Guid QuestionId, string? AnswerText);

public record SubmitQuizAttemptCommand(
    Guid AttemptId,
    IReadOnlyList<AnswerInput> Answers) : IRequest<Result<decimal?>>;

public class SubmitQuizAttemptHandler : IRequestHandler<SubmitQuizAttemptCommand, Result<decimal?>>
{
    private readonly ILmsDbContext _db;

    public SubmitQuizAttemptHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<decimal?>> Handle(SubmitQuizAttemptCommand cmd, CancellationToken ct)
    {
        var attempt = await _db.QuizAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == cmd.AttemptId, ct);
        if (attempt is null)
            return Result.Failure<decimal?>("Attempt not found.");

        if (attempt.IsCompleted)
            return Result.Failure<decimal?>("Attempt already submitted.");

        var quiz = await _db.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == attempt.QuizId, ct);
        if (quiz is null)
            return Result.Failure<decimal?>("Quiz not found.");

        var questionMap = quiz.Questions.ToDictionary(q => q.Id);
        var now = DateTime.UtcNow;

        foreach (var input in cmd.Answers)
        {
            if (!questionMap.TryGetValue(input.QuestionId, out var question))
                continue;

            bool? isCorrect = null;
            decimal? marksAwarded = null;

            if (question.QuestionType == QuestionType.MultipleChoice || question.QuestionType == QuestionType.TrueFalse)
            {
                isCorrect = input.AnswerText == question.CorrectAnswer;
                marksAwarded = isCorrect == true ? question.Marks : 0m;
            }
            // ShortAnswer: isCorrect stays null, marksAwarded stays null — manual grading required

            _db.QuizAnswers.Add(new QuizAnswer
            {
                TenantId     = attempt.TenantId,
                AttemptId    = attempt.Id,
                QuestionId   = input.QuestionId,
                AnswerText   = input.AnswerText,
                IsCorrect    = isCorrect,
                MarksAwarded = marksAwarded
            });
        }

        // Sum only objectively graded marks; short answers contribute 0 until manually graded
        var totalMarks = cmd.Answers
            .Join(quiz.Questions, a => a.QuestionId, q => q.Id, (a, q) => (Answer: a, Question: q))
            .Where(x => x.Question.QuestionType != QuestionType.ShortAnswer)
            .Sum(x => string.Equals(x.Answer.AnswerText, x.Question.CorrectAnswer, StringComparison.Ordinal) ? x.Question.Marks : 0m);

        attempt.TotalMarks   = totalMarks;
        attempt.IsCompleted  = true;
        attempt.SubmittedAt  = now;

        // Update student progress for the quiz
        var progress = await _db.StudentProgresses
            .FirstOrDefaultAsync(p => p.TenantId == attempt.TenantId && p.StudentId == attempt.StudentId && p.SubjectId == quiz.SubjectId && p.BatchId == quiz.BatchId, ct);

        if (progress is null)
        {
            progress = new StudentProgress
            {
                TenantId  = attempt.TenantId,
                StudentId = attempt.StudentId,
                SubjectId = quiz.SubjectId,
                BatchId   = quiz.BatchId
            };
            _db.StudentProgresses.Add(progress);
        }

        // Recalculate running average: (oldAvg * oldCount + newScore) / (oldCount + 1)
        var maxPossible = quiz.Questions
            .Where(q => q.QuestionType != QuestionType.ShortAnswer)
            .Sum(q => q.Marks);
        var scorePercent = maxPossible > 0 ? (totalMarks / maxPossible) * 100m : 0m;
        progress.AverageQuizScore = progress.QuizzesTaken == 0
            ? scorePercent
            : (progress.AverageQuizScore * progress.QuizzesTaken + scorePercent) / (progress.QuizzesTaken + 1);
        progress.QuizzesTaken++;
        progress.LastActivityAt = now;

        await _db.SaveChangesAsync(ct);
        return Result.Success<decimal?>(totalMarks);
    }
}
