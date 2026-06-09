using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record FinaliseAqarCommand(Guid AqarId) : IRequest<Result>;

public class FinaliseAqarHandler : IRequestHandler<FinaliseAqarCommand, Result>
{
    private readonly INaacDbContext _db;

    public FinaliseAqarHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(FinaliseAqarCommand request, CancellationToken cancellationToken)
    {
        var aqar = await _db.AqarReports
            .Include(a => a.Sections)
            .FirstOrDefaultAsync(a => a.Id == request.AqarId, cancellationToken);

        if (aqar is null)
            return Result.Failure("AQAR not found.");

        if (aqar.Sections.Any(s => s.Status != AqarStatus.Approved))
            return Result.Failure("All sections must be Approved before finalising.");

        aqar.Status = AqarStatus.Approved;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
