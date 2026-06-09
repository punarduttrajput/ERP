using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Library.Application.Commands;

public record AddBookCommand(
    string ISBN,
    string Title,
    string Authors,
    string? Publisher,
    int? PublicationYear,
    string? Edition,
    string? Category,
    string Language,
    string? ShelfLocation,
    string? CoverImageUrl,
    bool IsbnLookup,
    Guid TenantId
) : IRequest<Result<Guid>>;

public class AddBookCommandHandler : IRequestHandler<AddBookCommand, Result<Guid>>
{
    private readonly ILibraryDbContext _db;

    public AddBookCommandHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(AddBookCommand request, CancellationToken cancellationToken)
    {
        // ISBN metadata auto-fill would call Open Library API
        // (https://openlibrary.org/api/books?bibkeys=ISBN:{isbn}&format=json&jscmd=data)
        // in production — populate Title/Authors/Publisher fields from response.
        // Actual HTTP call requires IHttpClientFactory wiring; fields accepted explicitly for now.

        var book = new Book
        {
            TenantId = request.TenantId,
            ISBN = request.ISBN,
            Title = request.Title,
            Authors = request.Authors,
            Publisher = request.Publisher,
            PublicationYear = request.PublicationYear,
            Edition = request.Edition,
            Category = request.Category,
            Language = string.IsNullOrWhiteSpace(request.Language) ? "English" : request.Language,
            ShelfLocation = request.ShelfLocation,
            CoverImageUrl = request.CoverImageUrl,
            TotalCopies = 0,
            AvailableCopies = 0
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(book.Id);
    }
}
