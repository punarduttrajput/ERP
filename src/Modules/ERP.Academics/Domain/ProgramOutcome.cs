using ERP.Shared.Domain;

namespace ERP.Academics.Domain;

public class ProgramOutcome : TenantEntity
{
    public Guid ProgramId { get; set; }
    public string Code { get; set; } = default!;
    public string Description { get; set; } = default!;
}
