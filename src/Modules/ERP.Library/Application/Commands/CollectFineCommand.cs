using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Commands;

public record CollectFineCommand(
    Guid FineId,
    DateTime CollectedAt
) : IRequest<Result>;

public class CollectFineCommandHandler : IRequestHandler<CollectFineCommand, Result>
{
    private readonly ILibraryDbContext _db;

    public CollectFineCommandHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(CollectFineCommand request, CancellationToken cancellationToken)
    {
        var fine = await _db.LibraryFines
            .FirstOrDefaultAsync(x => x.Id == request.FineId, cancellationToken);

        if (fine is null)
            return Result.Failure("Fine not found.");

        if (fine.Status != FineStatus.Pending)
            return Result.Failure("Only pending fines can be collected.");

        fine.Status = FineStatus.Collected;
        fine.CollectedAt = request.CollectedAt;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
