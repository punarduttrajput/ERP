using ERP.Alumni.Domain;
using ERP.Alumni.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Alumni.Application.Commands;

public record RegisterAlumniCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    int GraduationYear,
    string ProgramName,
    string? BatchName,
    Guid? StudentId
) : IRequest<Result<Guid>>;

public class RegisterAlumniHandler : IRequestHandler<RegisterAlumniCommand, Result<Guid>>
{
    private readonly IAlumniDbContext _db;

    public RegisterAlumniHandler(IAlumniDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RegisterAlumniCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.AlumniProfiles
            .AnyAsync(x => x.TenantId == request.TenantId && x.Email == request.Email, cancellationToken);

        if (exists)
            return Result.Failure<Guid>("An alumni with this email is already registered.");

        var profile = new AlumniProfile
        {
            TenantId = request.TenantId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            GraduationYear = request.GraduationYear,
            ProgramName = request.ProgramName,
            BatchName = request.BatchName,
            StudentId = request.StudentId,
            IsDirectoryVisible = true,
            IsVerified = false
        };

        _db.AlumniProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(profile.Id);
    }
}
