using ERP.Shared.Domain;

namespace ERP.Finance.Domain;

public class Budget : TenantEntity
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public decimal TotalAllocated { get; set; }
    public decimal TotalSpent { get; set; } = 0m;
    public bool IsLocked { get; set; } = false;

    public ICollection<BudgetLine> Lines { get; set; } = new List<BudgetLine>();
}
