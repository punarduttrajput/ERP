using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Compliance.Application.Commands;

public record UpdateAisheReturnCommand(
    Guid TenantId,
    int AcademicYear,
    string? InstitutionType,
    int? EstablishmentYear,
    int? TotalProgrammes,
    int? TotalDepartments,
    int? TotalStudentsEnrolled,
    int? MaleStudents,
    int? FemaleStudents,
    int? ScStudents,
    int? StStudents,
    int? ObcStudents,
    int? TotalFaculty,
    int? MaleFaculty,
    int? FemaleFaculty,
    int? PhdFaculty,
    decimal? TotalBuiltAreaSqm,
    int? TotalLibraryBooks) : IRequest<Result>;

public class UpdateAisheReturnHandler : IRequestHandler<UpdateAisheReturnCommand, Result>
{
    private readonly IComplianceDbContext _db;

    public UpdateAisheReturnHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateAisheReturnCommand request, CancellationToken cancellationToken)
    {
        var r = _db.AisheReturns
            .FirstOrDefault(x => x.TenantId == request.TenantId && x.AcademicYear == request.AcademicYear && !x.IsDeleted);

        if (r is null)
            return Result.Failure("AISHE return not found for the specified academic year.");

        r.InstitutionType = request.InstitutionType;
        r.EstablishmentYear = request.EstablishmentYear;
        r.TotalProgrammes = request.TotalProgrammes;
        r.TotalDepartments = request.TotalDepartments;
        r.TotalStudentsEnrolled = request.TotalStudentsEnrolled;
        r.MaleStudents = request.MaleStudents;
        r.FemaleStudents = request.FemaleStudents;
        r.ScStudents = request.ScStudents;
        r.StStudents = request.StStudents;
        r.ObcStudents = request.ObcStudents;
        r.TotalFaculty = request.TotalFaculty;
        r.MaleFaculty = request.MaleFaculty;
        r.FemaleFaculty = request.FemaleFaculty;
        r.PhdFaculty = request.PhdFaculty;
        r.TotalBuiltAreaSqm = request.TotalBuiltAreaSqm;
        r.TotalLibraryBooks = request.TotalLibraryBooks;
        r.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
