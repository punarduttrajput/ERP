using ERP.Shared.Domain;

namespace ERP.Library.Domain;

public class BookIssue : TenantEntity
{
    public Guid CopyId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public MemberType MemberType { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateOnly DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.Active;
    public int RenewCount { get; set; } = 0;

    public BookCopy? Copy { get; set; }
}
