using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateAcademicYearCommand(
    string Label,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsCurrent) : IRequest<Result<Guid>>;

public class CreateAcademicYearHandler : IRequestHandler<CreateAcademicYearCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateAcademicYearHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateAcademicYearCommand request, CancellationToken cancellationToken)
    {
        if (request.StartDate >= request.EndDate)
            return Result.Failure<Guid>("StartDate must be before EndDate.");

        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        if (request.IsCurrent)
        {
            // Clear current flag from all other academic years for this tenant so there is exactly one current year.
            var currentYears = await _db.AcademicYears
                .Where(x => x.TenantId == tenantId && x.IsCurrent && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var y in currentYears)
                y.IsCurrent = false;
        }

        var academicYear = new AcademicYear
        {
            TenantId = tenantId,
            Label = request.Label,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent
        };

        _db.AcademicYears.Add(academicYear);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(academicYear.Id);
    }
}
