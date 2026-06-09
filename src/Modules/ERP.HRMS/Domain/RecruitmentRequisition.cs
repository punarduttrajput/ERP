using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class RecruitmentRequisition : TenantEntity
{
    public Guid DepartmentId { get; set; }
    public string Designation { get; set; } = string.Empty;
    public int NumberOfPositions { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = false;
    public DateOnly? ClosingDate { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
