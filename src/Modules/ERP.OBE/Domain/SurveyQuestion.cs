using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class SurveyQuestion : TenantEntity
{
    public Guid SurveyId { get; set; }
    public string CourseOutcomeCode { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public IndirectAttainmentSurvey? Survey { get; set; }
}
