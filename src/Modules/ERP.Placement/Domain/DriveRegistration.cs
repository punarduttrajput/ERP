using ERP.Shared.Domain;

namespace ERP.Placement.Domain;

public class DriveRegistration : TenantEntity
{
    public Guid DriveId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal StudentCgpa { get; set; }
    public int ActiveBacklogs { get; set; }
    public string Branch { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public RegistrationStatus Status { get; set; }
    public DateTime? InterviewScheduledAt { get; set; }
    public string? InterviewNotes { get; set; }
    public decimal? OfferLpa { get; set; }

    public PlacementDrive? Drive { get; set; }
    public PlacementOffer? Offer { get; set; }
}
