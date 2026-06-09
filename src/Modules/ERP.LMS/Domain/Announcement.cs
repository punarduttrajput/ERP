using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class Announcement : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid PostedBy { get; set; }
    public bool IsVisible { get; set; } = true;
}
