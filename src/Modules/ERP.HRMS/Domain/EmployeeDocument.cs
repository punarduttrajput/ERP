using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class EmployeeDocument : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public Employee? Employee { get; set; }
}
