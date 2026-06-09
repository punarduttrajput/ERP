using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record PostJobCommand(Guid RequisitionId, Guid TenantId) : IRequest<Result>;

public class PostJobHandler : IRequestHandler<PostJobCommand, Result>
{
    private readonly IHrmsDbContext _db;

    public PostJobHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(PostJobCommand request, CancellationToken cancellationToken)
    {
        var req = await _db.RecruitmentRequisitions
            .FirstOrDefaultAsync(r => r.Id == request.RequisitionId && r.TenantId == request.TenantId, cancellationToken);

        if (req is null)
            return Result.Failure("Requisition not found.");

        req.IsPublished = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
