using ERP.Shared.Domain;

namespace ERP.Research.Domain;

public class GrantDisbursement : TenantEntity
{
    public Guid GrantId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DisbursedAt { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public Grant? Grant { get; set; }
}
