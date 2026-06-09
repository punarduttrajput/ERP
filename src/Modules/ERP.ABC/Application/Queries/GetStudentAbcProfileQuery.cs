using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Queries;

public record StudentAbcProfileDto(
    Guid Id,
    Guid StudentId,
    string AbcId,
    bool IsVerified,
    DateTime? VerifiedAt,
    string? RegistryStudentName,
    int TotalCreditsEarned,
    int TotalCreditsTransferredIn,
    int TotalCreditsTransferredOut,
    PathwayType? ActivePathwayType);

public record GetStudentAbcProfileQuery(Guid TenantId, Guid StudentId)
    : IRequest<Result<StudentAbcProfileDto>>;

public class GetStudentAbcProfileHandler : IRequestHandler<GetStudentAbcProfileQuery, Result<StudentAbcProfileDto>>
{
    private readonly IAbcDbContext _db;

    public GetStudentAbcProfileHandler(IAbcDbContext db) => _db = db;

    public async Task<Result<StudentAbcProfileDto>> Handle(GetStudentAbcProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == request.TenantId && x.StudentId == request.StudentId && !x.IsDeleted)
            .Select(x => new StudentAbcProfileDto(
                x.Id, x.StudentId, x.AbcId, x.IsVerified, x.VerifiedAt,
                x.RegistryStudentName, x.TotalCreditsEarned,
                x.TotalCreditsTransferredIn, x.TotalCreditsTransferredOut,
                x.ActivePathwayType))
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
            return Result<StudentAbcProfileDto>.Failure("ABC profile not found for this student.");

        return Result<StudentAbcProfileDto>.Success(profile);
    }
}
