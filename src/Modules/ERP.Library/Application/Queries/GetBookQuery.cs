using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Queries;

public record BookDetailDto(
    Guid Id,
    string ISBN,
    string Title,
    string Authors,
    string? Publisher,
    int? PublicationYear,
    string? Edition,
    string? Category,
    string Language,
    int TotalCopies,
    int AvailableCopies,
    string? ShelfLocation,
    string? CoverImageUrl,
    IReadOnlyList<BookCopyDto> Copies
);

public record BookCopyDto(
    Guid Id,
    string Barcode,
    CopyStatus Status,
    DateOnly AcquisitionDate,
    decimal? Price
);

public record GetBookQuery(Guid BookId) : IRequest<Result<BookDetailDto>>;

public class GetBookQueryHandler : IRequestHandler<GetBookQuery, Result<BookDetailDto>>
{
    private readonly ILibraryDbContext _db;

    public GetBookQueryHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result<BookDetailDto>> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        var book = await _db.Books
            .Include(x => x.Copies)
            .FirstOrDefaultAsync(x => x.Id == request.BookId, cancellationToken);

        if (book is null)
            return Result<BookDetailDto>.Failure("Book not found.");

        var dto = new BookDetailDto(
            book.Id, book.ISBN, book.Title, book.Authors, book.Publisher,
            book.PublicationYear, book.Edition, book.Category, book.Language,
            book.TotalCopies, book.AvailableCopies, book.ShelfLocation, book.CoverImageUrl,
            book.Copies
                .Where(c => !c.IsDeleted)
                .Select(c => new BookCopyDto(c.Id, c.Barcode, c.Status, c.AcquisitionDate, c.Price))
                .ToList()
        );

        return Result<BookDetailDto>.Success(dto);
    }
}
