using ERP.Shared.Domain;

namespace ERP.Admissions.Domain;

public class ApplicationDocument : TenantEntity
{
    public Guid ApplicationId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsVerified { get; set; } = false;
    public string? VerificationRemark { get; set; }

    public AdmissionApplication? Application { get; set; }
}
