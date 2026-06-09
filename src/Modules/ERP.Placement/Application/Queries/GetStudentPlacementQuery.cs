using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Queries;

public record StudentPlacementHistoryDto(
    Guid RegistrationId,
    Guid DriveId,
    string CompanyName,
    string JobRole,
    decimal PackageLpa,
    DateTime RegisteredAt,
    RegistrationStatus RegistrationStatus,
    decimal? OfferLpa,
    OfferStatus? OfferStatus
);

public record GetStudentPlacementQuery(Guid StudentId) : IRequest<Result<IReadOnlyList<StudentPlacementHistoryDto>>>;

public class GetStudentPlacementHandler : IRequestHandler<GetStudentPlacementQuery, Result<IReadOnlyList<StudentPlacementHistoryDto>>>
{
    private readonly IPlacementDbContext _db;

    public GetStudentPlacementHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<StudentPlacementHistoryDto>>> Handle(GetStudentPlacementQuery request, CancellationToken cancellationToken)
    {
        var registrations = await _db.Registrations
            .Where(x => x.StudentId == request.StudentId)
            .Include(x => x.Drive)
            .Include(x => x.Offer)
            .OrderByDescending(x => x.RegisteredAt)
            .ToListAsync(cancellationToken);

        var result = registrations.Select(r => new StudentPlacementHistoryDto(
            r.Id,
            r.DriveId,
            r.Drive!.CompanyName,
            r.Drive.JobRole,
            r.Drive.PackageLpa,
            r.RegisteredAt,
            r.Status,
            r.OfferLpa,
            r.Offer?.Status
        )).ToList();

        return Result.Success<IReadOnlyList<StudentPlacementHistoryDto>>(result);
    }
}
