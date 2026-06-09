using ERP.Shared.Domain;

namespace ERP.Research.Domain;

public class Grant : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public string FundingAgency { get; set; } = string.Empty;
    public string? GrantNumber { get; set; }
    public decimal SanctionedAmount { get; set; }
    public decimal DisbursedAmount { get; set; }
    public decimal UtilizedAmount { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public GrantStatus Status { get; set; }
    public Guid PrincipalInvestigatorId { get; set; }
    public Guid? ResearchProjectId { get; set; }
    public ICollection<GrantDisbursement> Disbursements { get; set; } = new List<GrantDisbursement>();
}
