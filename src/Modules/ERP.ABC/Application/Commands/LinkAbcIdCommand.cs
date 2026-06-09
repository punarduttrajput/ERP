using ERP.ABC.Domain;
using ERP.ABC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.ABC.Application.Commands;

public record LinkAbcIdCommand(Guid TenantId, Guid StudentId, string AbcId)
    : IRequest<Result<string>>;

public class LinkAbcIdHandler : IRequestHandler<LinkAbcIdCommand, Result<string>>
{
    private readonly IAbcDbContext _db;
    private readonly IDigiLockerService _digiLocker;

    public LinkAbcIdHandler(IAbcDbContext db, IDigiLockerService digiLocker)
    {
        _db = db;
        _digiLocker = digiLocker;
    }

    public async Task<Result<string>> Handle(LinkAbcIdCommand request, CancellationToken cancellationToken)
    {
        if (request.AbcId.Length != 12 || !request.AbcId.All(char.IsDigit))
            return Result<string>.Failure("ABC ID must be exactly 12 numeric digits (invalid format).");

        var (isValid, studentName) = await _digiLocker.VerifyAbcIdAsync(request.AbcId, cancellationToken);
        if (!isValid)
            return Result<string>.Failure("ABC ID could not be verified with the DigiLocker/ABC national registry.");

        var duplicate = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == request.TenantId && x.AbcId == request.AbcId && x.StudentId != request.StudentId && !x.IsDeleted, cancellationToken);
        if (duplicate)
            return Result<string>.Failure("This ABC ID is already linked to another student in this institution.");

        var profile = await _db.StudentAbcProfiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == request.TenantId && x.StudentId == request.StudentId && !x.IsDeleted, cancellationToken);

        if (profile is null)
        {
            profile = new StudentAbcProfile
            {
                TenantId = request.TenantId,
                StudentId = request.StudentId,
                AbcId = request.AbcId,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow,
                RegistryStudentName = studentName
            };
            await _db.StudentAbcProfiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.AbcId = request.AbcId;
            profile.IsVerified = true;
            profile.VerifiedAt = DateTime.UtcNow;
            profile.RegistryStudentName = studentName;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result<string>.Success(studentName ?? string.Empty);
    }
}
