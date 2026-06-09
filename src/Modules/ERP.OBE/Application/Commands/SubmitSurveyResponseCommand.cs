using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record SurveyAnswerItem(Guid QuestionId, int Score);

public record SubmitSurveyResponseCommand(
    Guid TenantId,
    Guid SurveyId,
    Guid StudentId,
    IReadOnlyList<SurveyAnswerItem> Answers) : IRequest<Result>;

public class SubmitSurveyResponseHandler : IRequestHandler<SubmitSurveyResponseCommand, Result>
{
    private readonly IObeDbContext _db;

    public SubmitSurveyResponseHandler(IObeDbContext db) => _db = db;

    public async Task<Result> Handle(SubmitSurveyResponseCommand request, CancellationToken cancellationToken)
    {
        var survey = await _db.IndirectSurveys.FirstOrDefaultAsync(
            x => x.Id == request.SurveyId, cancellationToken);

        if (survey is null)
            return Result.Failure("Survey not found.");

        if (!survey.IsPublished)
            return Result.Failure("Survey is not published.");

        if (survey.ClosedAt.HasValue)
            return Result.Failure("Survey is closed.");

        foreach (var answer in request.Answers)
        {
            if (answer.Score < 1 || answer.Score > 5)
                return Result.Failure($"Score must be 1-5, got {answer.Score}.");

            var alreadyAnswered = await _db.SurveyResponses.AnyAsync(
                x => x.TenantId == request.TenantId
                  && x.SurveyId == request.SurveyId
                  && x.StudentId == request.StudentId
                  && x.QuestionId == answer.QuestionId,
                cancellationToken);

            if (alreadyAnswered)
                return Result.Failure($"Student has already answered question {answer.QuestionId}.");

            _db.SurveyResponses.Add(new SurveyResponse
            {
                TenantId = request.TenantId,
                SurveyId = request.SurveyId,
                StudentId = request.StudentId,
                QuestionId = answer.QuestionId,
                Score = answer.Score,
                SubmittedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
