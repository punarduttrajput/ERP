using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Compliance.Application.Queries;

public record GetAisheReturnQuery(Guid TenantId, int AcademicYear) : IRequest<Result<AisheReturnDto>>;

public record AisheReturnDto(
    Guid Id,
    int AcademicYear,
    AisheReturnStatus Status,
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
    int? TotalLibraryBooks,
    DateTime? CompiledAt,
    DateTime? SubmittedAt,
    string? SubmissionReference);

public class GetAisheReturnHandler : IRequestHandler<GetAisheReturnQuery, Result<AisheReturnDto>>
{
    private readonly IComplianceDbContext _db;

    public GetAisheReturnHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<AisheReturnDto>> Handle(GetAisheReturnQuery request, CancellationToken cancellationToken)
    {
        var r = _db.AisheReturns
            .FirstOrDefault(x => x.TenantId == request.TenantId && x.AcademicYear == request.AcademicYear && !x.IsDeleted);

        if (r is null)
            return Result.Failure<AisheReturnDto>("AISHE return not found for the specified academic year.");

        return Result.Success(new AisheReturnDto(
            r.Id, r.AcademicYear, r.Status, r.InstitutionType, r.EstablishmentYear,
            r.TotalProgrammes, r.TotalDepartments, r.TotalStudentsEnrolled,
            r.MaleStudents, r.FemaleStudents, r.ScStudents, r.StStudents, r.ObcStudents,
            r.TotalFaculty, r.MaleFaculty, r.FemaleFaculty, r.PhdFaculty,
            r.TotalBuiltAreaSqm, r.TotalLibraryBooks, r.CompiledAt, r.SubmittedAt, r.SubmissionReference));
    }
}
