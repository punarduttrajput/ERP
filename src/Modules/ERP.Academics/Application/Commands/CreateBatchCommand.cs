using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateBatchCommand(
    Guid ProgramId,
    Guid AcademicYearId,
    string Name,
    int AdmissionYear) : IRequest<Result<Guid>>;

public class CreateBatchHandler : IRequestHandler<CreateBatchCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateBatchHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateBatchCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var programExists = await _db.AcademicPrograms
            .AnyAsync(x => x.Id == request.ProgramId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!programExists)
            return Result.Failure<Guid>("Program not found.");

        var yearExists = await _db.AcademicYears
            .AnyAsync(x => x.Id == request.AcademicYearId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!yearExists)
            return Result.Failure<Guid>("Academic year not found.");

        var batch = new Batch
        {
            TenantId = tenantId,
            ProgramId = request.ProgramId,
            AcademicYearId = request.AcademicYearId,
            Name = request.Name,
            AdmissionYear = request.AdmissionYear
        };

        _db.Batches.Add(batch);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(batch.Id);
    }
}
