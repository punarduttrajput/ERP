using ERP.Shared.Domain;

namespace ERP.Placement.Domain;

public class PlacementOffer : TenantEntity
{
    public Guid RegistrationId { get; set; }
    public Guid DriveId { get; set; }
    public Guid StudentId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobRole { get; set; } = string.Empty;
    public decimal OfferedPackageLpa { get; set; }
    public DateOnly? JoiningDate { get; set; }
    public OfferStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? OfferLetterBlobUrl { get; set; }

    public DriveRegistration? Registration { get; set; }
}
