using ERP.Shared.Domain;

namespace ERP.Compliance.Domain;

public class AisheReturn : TenantEntity
{
    public int AcademicYear { get; set; }
    public AisheReturnStatus Status { get; set; } = AisheReturnStatus.Draft;

    // Section A: Institution Profile
    public string? InstitutionType { get; set; }
    public int? EstablishmentYear { get; set; }

    // Section B: Programmes
    public int? TotalProgrammes { get; set; }
    public int? TotalDepartments { get; set; }

    // Section C: Students (auto-compiled from SIS)
    public int? TotalStudentsEnrolled { get; set; }
    public int? MaleStudents { get; set; }
    public int? FemaleStudents { get; set; }
    public int? ScStudents { get; set; }
    public int? StStudents { get; set; }
    public int? ObcStudents { get; set; }

    // Section D: Faculty (auto-compiled from HRMS)
    public int? TotalFaculty { get; set; }
    public int? MaleFaculty { get; set; }
    public int? FemaleFaculty { get; set; }
    public int? PhdFaculty { get; set; }

    // Section E: Infrastructure
    public decimal? TotalBuiltAreaSqm { get; set; }
    public int? TotalLibraryBooks { get; set; }

    public DateTime? CompiledAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? SubmissionReference { get; set; }
}
