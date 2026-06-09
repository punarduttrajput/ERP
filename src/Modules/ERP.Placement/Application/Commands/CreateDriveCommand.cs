using ERP.Placement.Domain;
using ERP.Placement.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Placement.Application.Commands;

public record CreateDriveCommand(
    Guid TenantId,
    Guid CompanyId,
    string JobRole,
    string? JobDescription,
    string? Location,
    decimal PackageLpa,
    decimal MinCgpa,
    int MaxBacklogs,
    string? EligibleBranches,
    DateOnly? DriveDate,
    DateOnly? RegistrationDeadline,
    DriveStatus Status,
    int AcademicYear
) : IRequest<Result<Guid>>;

public class CreateDriveHandler : IRequestHandler<CreateDriveCommand, Result<Guid>>
{
    private readonly IPlacementDbContext _db;

    public CreateDriveHandler(IPlacementDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateDriveCommand request, CancellationToken cancellationToken)
    {
        var company = await _db.Companies.FirstOrDefaultAsync(x => x.Id == request.CompanyId, cancellationToken);
        if (company is null)
            return Result.Failure<Guid>("Company not found.");

        var drive = new PlacementDrive
        {
            TenantId = request.TenantId,
            CompanyId = request.CompanyId,
            CompanyName = company.Name,
            JobRole = request.JobRole,
            JobDescription = request.JobDescription,
            Location = request.Location,
            PackageLpa = request.PackageLpa,
            MinCgpa = request.MinCgpa,
            MaxBacklogs = request.MaxBacklogs,
            EligibleBranches = request.EligibleBranches,
            DriveDate = request.DriveDate,
            RegistrationDeadline = request.RegistrationDeadline,
            Status = request.Status,
            AcademicYear = request.AcademicYear
        };

        _db.Drives.Add(drive);

        company.TotalDrives++;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(drive.Id);
    }
}
