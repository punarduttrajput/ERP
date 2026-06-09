using ERP.Shared.Domain;

namespace ERP.Fees.Domain;

public class FeeStructure : TenantEntity
{
    public Guid ProgramId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public int SemesterNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FeeComponent> Components { get; set; } = new List<FeeComponent>();
    public ICollection<InstallmentSchedule> InstallmentSchedules { get; set; } = new List<InstallmentSchedule>();
}
