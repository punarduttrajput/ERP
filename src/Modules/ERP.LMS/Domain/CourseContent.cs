using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class CourseContent : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ContentType ContentType { get; set; }
    public string? BlobUrl { get; set; }
    public string? ExternalUrl { get; set; }
    public int OrderIndex { get; set; }
    public bool IsVisible { get; set; } = true;
    public Guid UploadedBy { get; set; }
}
