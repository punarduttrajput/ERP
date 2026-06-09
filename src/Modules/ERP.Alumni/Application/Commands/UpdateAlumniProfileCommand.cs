using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record UpdateAlumniProfileCommand(
    Guid TenantId,
    Guid AlumniId,
    string? MobileNumber,
    string? CurrentEmployer,
    string? CurrentJobTitle,
    string? CurrentCity,
    string? CurrentCountry,
    string? LinkedInUrl,
    string? AvatarUrl
) : IRequest<Result>;

public class UpdateAlumniProfileHandler : IRequestHandler<UpdateAlumniProfileCommand, Result>
{
    private readonly IAlumniDbContext _db;

    public UpdateAlumniProfileHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateAlumniProfileCommand request, CancellationToken cancellationToken)
    {
        var profile = await _db.AlumniProfiles
            .FirstOrDefaultAsync(x => x.Id == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (profile is null)
            return Result.Failure("Alumni profile not found.");

        if (request.MobileNumber is not null) profile.MobileNumber = request.MobileNumber;
        if (request.CurrentEmployer is not null) profile.CurrentEmployer = request.CurrentEmployer;
        if (request.CurrentJobTitle is not null) profile.CurrentJobTitle = request.CurrentJobTitle;
        if (request.CurrentCity is not null) profile.CurrentCity = request.CurrentCity;
        if (request.CurrentCountry is not null) profile.CurrentCountry = request.CurrentCountry;
        if (request.LinkedInUrl is not null) profile.LinkedInUrl = request.LinkedInUrl;
        if (request.AvatarUrl is not null) profile.AvatarUrl = request.AvatarUrl;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
