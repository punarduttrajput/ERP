using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Commands;

public record RegisterForDriveCommand(
    Guid TenantId,
    Guid DriveId,
    Guid StudentId,
    string StudentName,
    decimal StudentCgpa,
    int ActiveBacklogs,
    string Branch
) : IRequest<Result<Guid>>;

public class RegisterForDriveHandler : IRequestHandler<RegisterForDriveCommand, Result<Guid>>
{
    private readonly IPlacementDbContext _db;

    public RegisterForDriveHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(RegisterForDriveCommand request, CancellationToken cancellationToken)
    {
        var drive = await _db.Drives.FirstOrDefaultAsync(x => x.Id == request.DriveId, cancellationToken);
        if (drive is null)
            return Result.Failure<Guid>("Drive not found.");

        if (drive.Status != DriveStatus.Open)
            return Result.Failure<Guid>("Drive is not open for registration.");

        if (drive.RegistrationDeadline.HasValue && DateOnly.FromDateTime(DateTime.UtcNow) > drive.RegistrationDeadline.Value)
            return Result.Failure<Guid>("Registration deadline has passed.");

        if (request.StudentCgpa < drive.MinCgpa)
            return Result.Failure<Guid>("CGPA below minimum requirement.");

        if (request.ActiveBacklogs > drive.MaxBacklogs)
            return Result.Failure<Guid>("Exceeds maximum allowed backlogs.");

        // null/empty EligibleBranches means all branches are eligible
        if (!string.IsNullOrWhiteSpace(drive.EligibleBranches))
        {
            var eligible = drive.EligibleBranches.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!eligible.Contains(request.Branch, StringComparer.OrdinalIgnoreCase))
                return Result.Failure<Guid>("Branch not eligible for this drive.");
        }

        var alreadyRegistered = await _db.Registrations.AnyAsync(
            x => x.DriveId == request.DriveId && x.StudentId == request.StudentId,
            cancellationToken);
        if (alreadyRegistered)
            return Result.Failure<Guid>("Student is already registered for this drive.");

        var registration = new DriveRegistration
        {
            TenantId = request.TenantId,
            DriveId = request.DriveId,
            StudentId = request.StudentId,
            StudentName = request.StudentName,
            StudentCgpa = request.StudentCgpa,
            ActiveBacklogs = request.ActiveBacklogs,
            Branch = request.Branch,
            RegisteredAt = DateTime.UtcNow,
            Status = RegistrationStatus.Registered
        };

        _db.Registrations.Add(registration);
        drive.TotalRegistrations++;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(registration.Id);
    }
}
