using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class ForumThread : TenantEntity
{
    public Guid SubjectId { get; set; }
    public Guid BatchId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public bool IsPinned { get; set; } = false;
    public int ReplyCount { get; set; } = 0;
    public ICollection<ForumReply> Replies { get; set; } = new List<ForumReply>();
}
