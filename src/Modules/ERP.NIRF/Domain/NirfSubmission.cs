using ERP.Shared.Domain;

namespace ERP.NIRF.Domain;

public class NirfSubmission : TenantEntity
{
    public int RankingYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Draft;
    public decimal? OverallScore { get; set; }
    public int? EstimatedRank { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public ICollection<NirfParameterScore> ParameterScores { get; set; } = new List<NirfParameterScore>();
}
