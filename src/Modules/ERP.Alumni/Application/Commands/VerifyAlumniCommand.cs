using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record VerifyAlumniCommand(Guid TenantId, Guid AlumniId) : IRequest<Result>;

public class VerifyAlumniHandler : IRequestHandler<VerifyAlumniCommand, Result>
{
    private readonly IAlumniDbContext _db;

    public VerifyAlumniHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result> Handle(VerifyAlumniCommand request, CancellationToken cancellationToken)
    {
        var profile = await _db.AlumniProfiles
            .FirstOrDefaultAsync(x => x.Id == request.AlumniId && x.TenantId == request.TenantId, cancellationToken);

        if (profile is null)
            return Result.Failure("Alumni profile not found.");

        profile.IsVerified = true;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
