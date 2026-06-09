using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Commands;

public record AddBookCopyCommand(
    Guid BookId,
    string Barcode,
    DateOnly AcquisitionDate,
    decimal? Price,
    Guid TenantId
) : IRequest<Result<Guid>>;

public class AddBookCopyCommandHandler : IRequestHandler<AddBookCopyCommand, Result<Guid>>
{
    private readonly ILibraryDbContext _db;

    public AddBookCopyCommandHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(AddBookCopyCommand request, CancellationToken cancellationToken)
    {
        var book = await _db.Books
            .FirstOrDefaultAsync(x => x.Id == request.BookId, cancellationToken);

        if (book is null)
            return Result<Guid>.Failure("Book not found.");

        var copy = new BookCopy
        {
            TenantId = request.TenantId,
            BookId = request.BookId,
            Barcode = request.Barcode,
            Status = CopyStatus.Available,
            AcquisitionDate = request.AcquisitionDate,
            Price = request.Price
        };

        book.TotalCopies++;
        book.AvailableCopies++;

        _db.BookCopies.Add(copy);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(copy.Id);
    }
}
