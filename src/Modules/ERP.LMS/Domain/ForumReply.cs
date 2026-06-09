using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class ForumReply : TenantEntity
{
    public Guid ThreadId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public ForumThread? Thread { get; set; }
}
