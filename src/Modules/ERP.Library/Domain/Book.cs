using ERP.Shared.Domain;

namespace ERP.Library.Domain;

public class Book : TenantEntity
{
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Edition { get; set; }
    public string? Category { get; set; }
    public string Language { get; set; } = "English";
    public int TotalCopies { get; set; } = 0;
    public int AvailableCopies { get; set; } = 0;
    public string? ShelfLocation { get; set; }
    public string? CoverImageUrl { get; set; }

    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
}
