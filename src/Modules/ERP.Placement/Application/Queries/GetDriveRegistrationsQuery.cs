using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Queries;

public record DriveRegistrationDto(
    Guid Id,
    Guid DriveId,
    Guid StudentId,
    string StudentName,
    decimal StudentCgpa,
    int ActiveBacklogs,
    string Branch,
    DateTime RegisteredAt,
    RegistrationStatus Status,
    DateTime? InterviewScheduledAt,
    string? InterviewNotes,
    decimal? OfferLpa
);

public record GetDriveRegistrationsQuery(
    Guid DriveId,
    int Page,
    int PageSize
) : IRequest<PagedResult<DriveRegistrationDto>>;

public class GetDriveRegistrationsHandler : IRequestHandler<GetDriveRegistrationsQuery, PagedResult<DriveRegistrationDto>>
{
    private readonly IPlacementDbContext _db;

    public GetDriveRegistrationsHandler(IPlacementDbContext db) => _db = db;

    public async Task<PagedResult<DriveRegistrationDto>> Handle(GetDriveRegistrationsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Registrations.Where(x => x.DriveId == request.DriveId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(x => x.StudentName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new DriveRegistrationDto(
                x.Id, x.DriveId, x.StudentId, x.StudentName,
                x.StudentCgpa, x.ActiveBacklogs, x.Branch,
                x.RegisteredAt, x.Status, x.InterviewScheduledAt,
                x.InterviewNotes, x.OfferLpa))
            .ToListAsync(cancellationToken);

        return new PagedResult<DriveRegistrationDto>(items, total, request.Page, request.PageSize);
    }
}
