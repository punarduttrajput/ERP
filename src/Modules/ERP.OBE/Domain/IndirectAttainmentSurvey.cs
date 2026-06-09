using ERP.Shared.Domain;

namespace ERP.OBE.Domain;

public class IndirectAttainmentSurvey : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid SemesterId { get; set; }
    public int AcademicYear { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateTime? ClosedAt { get; set; }
    public decimal? AggregatedScore { get; set; }
    public ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
}
