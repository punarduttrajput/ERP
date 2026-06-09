using ERP.Library.Domain;
using ERP.Library.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Library.Application.Commands;

public record WaiveFineCommand(
    Guid FineId,
    Guid WaivedBy,
    string Reason
) : IRequest<Result>;

public class WaiveFineCommandHandler : IRequestHandler<WaiveFineCommand, Result>
{
    private readonly ILibraryDbContext _db;

    public WaiveFineCommandHandler(ILibraryDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(WaiveFineCommand request, CancellationToken cancellationToken)
    {
        var fine = await _db.LibraryFines
            .FirstOrDefaultAsync(x => x.Id == request.FineId, cancellationToken);

        if (fine is null)
            return Result.Failure("Fine not found.");

        if (fine.Status != FineStatus.Pending)
            return Result.Failure("Only pending fines can be waived.");

        fine.Status = FineStatus.Waived;
        fine.WaivedBy = request.WaivedBy;
        fine.WaivedReason = request.Reason;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
