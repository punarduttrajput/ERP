using ERP.Shared.Domain;

namespace ERP.HRMS.Domain;

public class JobApplication : TenantEntity
{
    public Guid RequisitionId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string ApplicantEmail { get; set; } = string.Empty;
    public string? ApplicantMobile { get; set; }
    public string? ResumeBlobUrl { get; set; }
    public RecruitmentStatus Status { get; set; } = RecruitmentStatus.Applied;
    public DateTime? InterviewDate { get; set; }
    public string? InterviewNotes { get; set; }
    public decimal? OfferSalary { get; set; }
    public string? RejectionReason { get; set; }

    public RecruitmentRequisition? Requisition { get; set; }
}
