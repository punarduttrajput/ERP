using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.OBE.Application.Commands;

public record SurveyQuestionItem(string CourseOutcomeCode, string QuestionText, int OrderIndex);

public record CreateSurveyCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid SemesterId,
    int AcademicYear,
    string Title,
    IReadOnlyList<SurveyQuestionItem> Questions) : IRequest<Result<Guid>>;

public class CreateSurveyHandler : IRequestHandler<CreateSurveyCommand, Result<Guid>>
{
    private readonly IObeDbContext _db;

    public CreateSurveyHandler(IObeDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateSurveyCommand request, CancellationToken cancellationToken)
    {
        if (request.Questions.Count == 0)
            return Result<Guid>.Failure("Survey must have at least one question.");

        var survey = new IndirectAttainmentSurvey
        {
            TenantId = request.TenantId,
            SubjectId = request.SubjectId,
            SemesterId = request.SemesterId,
            AcademicYear = request.AcademicYear,
            Title = request.Title
        };

        foreach (var q in request.Questions)
        {
            survey.Questions.Add(new SurveyQuestion
            {
                TenantId = request.TenantId,
                SurveyId = survey.Id,
                CourseOutcomeCode = q.CourseOutcomeCode,
                QuestionText = q.QuestionText,
                OrderIndex = q.OrderIndex
            });
        }

        _db.IndirectSurveys.Add(survey);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(survey.Id);
    }
}
