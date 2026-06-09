using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.HRMS.Application.Commands;

public record CreateRequisitionCommand(
    Guid TenantId,
    Guid DepartmentId,
    string Designation,
    int NumberOfPositions,
    string JobDescription,
    DateOnly? ClosingDate
) : IRequest<Result<Guid>>;

public class CreateRequisitionHandler : IRequestHandler<CreateRequisitionCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;

    public CreateRequisitionHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateRequisitionCommand request, CancellationToken cancellationToken)
    {
        var req = new RecruitmentRequisition
        {
            TenantId = request.TenantId,
            DepartmentId = request.DepartmentId,
            Designation = request.Designation,
            NumberOfPositions = request.NumberOfPositions,
            JobDescription = request.JobDescription,
            ClosingDate = request.ClosingDate
        };

        _db.RecruitmentRequisitions.Add(req);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(req.Id);
    }
}
