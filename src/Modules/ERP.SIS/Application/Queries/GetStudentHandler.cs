using ERP.Shared.Application.Common;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Queries;

public class GetStudentHandler : IRequestHandler<GetStudentQuery, Result<StudentDto>>
{
    private readonly ISisDbContext _db;

    public GetStudentHandler(ISisDbContext db)
    {
        _db = db;
    }

    public async Task<Result<StudentDto>> Handle(GetStudentQuery request, CancellationToken cancellationToken)
    {
        var s = await _db.Students.FirstOrDefaultAsync(x => x.Id == request.StudentId, cancellationToken);
        if (s is null)
            return Result.Failure<StudentDto>("Student not found.");

        return Result.Success(new StudentDto(
            s.Id, s.StudentNumber, s.ApplicationId, s.ProgramId, s.ProgramName,
            s.AcademicYear, s.EnrolledAt, s.FirstName, s.LastName, s.MiddleName,
            s.Email, s.MobileNumber, s.DateOfBirth, s.Gender, s.BloodGroup,
            s.PermanentAddress, s.CurrentAddress, s.Category, s.Semester, s.IsActive
        ));
    }
}
