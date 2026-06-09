using ERP.Shared.Domain;

namespace ERP.ABC.Domain;

public class CreditTransfer : TenantEntity
{
    public Guid StudentId { get; set; }
    public string AbcId { get; set; } = string.Empty;
    public TransferDirection Direction { get; set; }
    public string SourceInstitution { get; set; } = string.Empty;
    public string? DestinationInstitution { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int CreditsRequested { get; set; }
    public int? CreditsApproved { get; set; }
    public int AcademicYear { get; set; }
    public TransferStatus Status { get; set; } = TransferStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? AbcRegistryReference { get; set; }
}
