using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record ToggleAlumniVisibilityCommand(Guid TenantId, Guid AlumniId, bool IsDirectoryVisible) : IRequest<Result>;

public class ToggleAlumniVisibilityHandler : IRequestHandler<ToggleAlumniVisibilityCommand, Result>
{
    private readonly IAlumniDbContext _db;

    public ToggleAlumniVisibilityHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result> Handle(ToggleAlumniVisibilityCommand request, CancellationToken cancellationToken)
    {
        var profile = await _db.AlumniProfiles
            .FirstOrDefaultAsync(x => x.Id == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (profile is null)
            return Result.Failure("Alumni profile not found.");

        profile.IsDirectoryVisible = request.IsDirectoryVisible;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
