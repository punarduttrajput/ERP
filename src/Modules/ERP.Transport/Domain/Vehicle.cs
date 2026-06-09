using ERP.Shared.Domain;

namespace ERP.Transport.Domain;

public class Vehicle : TenantEntity
{
    public string RegistrationNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public DateOnly FitnessExpiryDate { get; set; }
    public DateOnly InsuranceExpiryDate { get; set; }
    public DateOnly PollutionExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
}
