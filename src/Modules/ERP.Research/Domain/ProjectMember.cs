using ERP.Shared.Domain;

namespace ERP.Research.Domain;

public class ProjectMember : TenantEntity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public MemberRole Role { get; set; }
    public DateOnly JoinedAt { get; set; }
    public DateOnly? LeftAt { get; set; }
    public ResearchProject? Project { get; set; }
}
