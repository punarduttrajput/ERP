using ERP.Shared.Domain;

namespace ERP.Library.Domain;

public class LibraryPolicy : TenantEntity
{
    public MemberType MemberType { get; set; }
    public int MaxBooksAllowed { get; set; }
    public int LoanPeriodDays { get; set; }
    public decimal FinePerDay { get; set; }
    public int MaxRenewals { get; set; } = 1;
    public int GracePeriodDays { get; set; } = 0;
}
