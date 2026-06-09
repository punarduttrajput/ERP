using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Queries;

public record AlumniProfileDto(
    Guid Id,
    Guid? StudentId,
    string FirstName,
    string LastName,
    string Email,
    string? MobileNumber,
    int GraduationYear,
    string ProgramName,
    string? BatchName,
    string? CurrentEmployer,
    string? CurrentJobTitle,
    string? CurrentCity,
    string CurrentCountry,
    string? LinkedInUrl,
    bool IsDirectoryVisible,
    bool IsVerified,
    string? AvatarUrl
);

public record GetAlumniProfileQuery(Guid TenantId, Guid AlumniId) : IRequest<Result<AlumniProfileDto>>;

public class GetAlumniProfileHandler : IRequestHandler<GetAlumniProfileQuery, Result<AlumniProfileDto>>
{
    private readonly IAlumniDbContext _db;

    public GetAlumniProfileHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<AlumniProfileDto>> Handle(GetAlumniProfileQuery request, CancellationToken cancellationToken)
    {
        var profile = await _db.AlumniProfiles
            .FirstOrDefaultAsync(x => x.Id == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (profile is null)
            return Result.Failure<AlumniProfileDto>("Alumni profile not found.");

        var dto = new AlumniProfileDto(
            profile.Id,
            profile.StudentId,
            profile.FirstName,
            profile.LastName,
            profile.Email,
            profile.MobileNumber,
            profile.GraduationYear,
            profile.ProgramName,
            profile.BatchName,
            profile.CurrentEmployer,
            profile.CurrentJobTitle,
            profile.CurrentCity,
            profile.CurrentCountry,
            profile.LinkedInUrl,
            profile.IsDirectoryVisible,
            profile.IsVerified,
            profile.AvatarUrl
        );

        return Result.Success(dto);
    }
}
