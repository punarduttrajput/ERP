using ERP.Shared.Domain;

namespace ERP.NIRF.Domain;

public class NirfRankEntry : TenantEntity
{
    public int RankingYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public int? Rank { get; set; }
    public decimal? Score { get; set; }
    public decimal? TeachingLearningScore { get; set; }
    public decimal? ResearchScore { get; set; }
    public decimal? GraduationOutcomesScore { get; set; }
    public decimal? OutreachScore { get; set; }
    public decimal? PerceptionScore { get; set; }
}
