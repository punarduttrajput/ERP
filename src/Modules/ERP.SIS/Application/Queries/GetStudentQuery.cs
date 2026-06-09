using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.SIS.Application.Queries;

public record GetStudentQuery(Guid StudentId) : IRequest<Result<StudentDto>>;

public record StudentDto(
    Guid Id,
    string StudentNumber,
    Guid ApplicationId,
    Guid ProgramId,
    string ProgramName,
    int AcademicYear,
    DateTime EnrolledAt,
    string FirstName,
    string LastName,
    string? MiddleName,
    string Email,
    string MobileNumber,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? PermanentAddress,
    string? CurrentAddress,
    string Category,
    int Semester,
    bool IsActive
);
