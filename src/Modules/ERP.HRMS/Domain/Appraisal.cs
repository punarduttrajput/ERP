using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class Appraisal : TenantEntity
{
    public Guid EmployeeId { get; set; }
    public int ReviewYear { get; set; }
    public AppraisalStatus Status { get; set; } = AppraisalStatus.Draft;
    public string? SelfAssessment { get; set; }
    public decimal? SelfRating { get; set; }
    public string? ManagerReview { get; set; }
    public decimal? ManagerRating { get; set; }
    public string? HrComments { get; set; }
    public decimal? FinalRating { get; set; }
    public Guid? FinalReviewedBy { get; set; }
    public DateTime? FinalReviewedAt { get; set; }
}
