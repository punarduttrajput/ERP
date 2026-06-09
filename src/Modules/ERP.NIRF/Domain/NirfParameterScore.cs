using ERP.Shared.Domain;

namespace ERP.NIRF.Domain;

public class NirfParameterScore : TenantEntity
{
    public Guid SubmissionId { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public decimal RawScore { get; set; }
    public decimal WeightedScore { get; set; }
    public decimal Weight { get; set; }
    public string DataJson { get; set; } = string.Empty;
    public bool IsManualOverride { get; set; } = false;
    public NirfSubmission? Submission { get; set; }
}
