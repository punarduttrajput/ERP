using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Commands;

public record ApprovePathwayCommand(Guid TenantId, Guid PathwayId, Guid ApprovedBy)
    : IRequest<Result>;

public class ApprovePathwayHandler : IRequestHandler<ApprovePathwayCommand, Result>
{
    private readonly IAbcDbContext _db;

    public ApprovePathwayHandler(IAbcDbContext db) => _db = db;

    public async Task<Result> Handle(ApprovePathwayCommand request, CancellationToken cancellationToken)
    {
        var pathway = await _db.AcademicPathways
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.PathwayId && x.TenantId == request.TenantId && !x.IsDeleted, cancellationToken);

        if (pathway is null)
            return Result.Failure("Academic pathway not found.");
        if (pathway.Status != "Requested")
            return Result.Failure("Only pathways in Requested status can be approved.");

        pathway.Status = "Approved";
        pathway.ApprovedAt = DateTime.UtcNow;
        pathway.ApprovedBy = request.ApprovedBy;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
