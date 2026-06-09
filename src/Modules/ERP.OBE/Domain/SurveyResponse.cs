using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class SurveyResponse : TenantEntity
{
    public Guid SurveyId { get; set; }
    public Guid StudentId { get; set; }
    public Guid QuestionId { get; set; }
    public int Score { get; set; }
    public DateTime SubmittedAt { get; set; }
}
