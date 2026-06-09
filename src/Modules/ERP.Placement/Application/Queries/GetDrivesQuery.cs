using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Queries;

public record DriveDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string JobRole,
    string? Location,
    decimal PackageLpa,
    decimal MinCgpa,
    int MaxBacklogs,
    string? EligibleBranches,
    DateOnly? DriveDate,
    DateOnly? RegistrationDeadline,
    DriveStatus Status,
    int AcademicYear,
    int TotalRegistrations,
    int TotalSelected
);

public record GetDrivesQuery(
    int Page,
    int PageSize,
    DriveStatus? Status,
    int? AcademicYear,
    Guid? CompanyId
) : IRequest<PagedResult<DriveDto>>;

public class GetDrivesHandler : IRequestHandler<GetDrivesQuery, PagedResult<DriveDto>>
{
    private readonly IPlacementDbContext _db;

    public GetDrivesHandler(IPlacementDbContext db) => _db = db;

    public async Task<PagedResult<DriveDto>> Handle(GetDrivesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Drives.AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.AcademicYear.HasValue)
            query = query.Where(x => x.AcademicYear == request.AcademicYear.Value);

        if (request.CompanyId.HasValue)
            query = query.Where(x => x.CompanyId == request.CompanyId.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new DriveDto(
                x.Id, x.CompanyId, x.CompanyName, x.JobRole,
                x.Location, x.PackageLpa, x.MinCgpa, x.MaxBacklogs,
                x.EligibleBranches, x.DriveDate, x.RegistrationDeadline,
                x.Status, x.AcademicYear, x.TotalRegistrations, x.TotalSelected))
            .ToListAsync(cancellationToken);

        return new PagedResult<DriveDto>(items, total, request.Page, request.PageSize);
    }
}
