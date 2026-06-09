using ERP.Shared.Domain;

namespace ERP.Library.Domain;

public class BookCopy : TenantEntity
{
    public Guid BookId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public CopyStatus Status { get; set; } = CopyStatus.Available;
    public DateOnly AcquisitionDate { get; set; }
    public decimal? Price { get; set; }

    public Book? Book { get; set; }
}
