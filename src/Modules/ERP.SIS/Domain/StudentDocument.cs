using ERP.Shared.Domain;

namespace ERP.SIS.Domain;

public class StudentDocument : TenantEntity
{
    public Guid StudentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public Student? Student { get; set; }
}
