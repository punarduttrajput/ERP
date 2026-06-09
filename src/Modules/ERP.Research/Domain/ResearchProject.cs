using ERP.Shared.Domain;

namespace ERP.Research.Domain;

public class ResearchProject : TenantEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid PrincipalInvestigatorId { get; set; }
    public string PrincipalInvestigatorName { get; set; } = string.Empty;
    public string FundingAgency { get; set; } = string.Empty;
    public string? FundingScheme { get; set; }
    public decimal SanctionedAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public string? SanctionNumber { get; set; }
    public string? Abstract { get; set; }
    public string? Domain { get; set; }
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
