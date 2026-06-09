using ERP.Shared.Application.Common;
using ERP.SIS.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.SIS.Application.Commands;

public class UpdateStudentHandler : IRequestHandler<UpdateStudentCommand, Result>
{
    private readonly ISisDbContext _db;

    public UpdateStudentHandler(ISisDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateStudentCommand request, CancellationToken cancellationToken)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == request.StudentId, cancellationToken);
        if (student is null)
            return Result.Failure("Student not found.");

        student.FirstName = request.FirstName;
        student.LastName = request.LastName;
        student.MiddleName = request.MiddleName;
        student.MobileNumber = request.MobileNumber;
        student.DateOfBirth = request.DateOfBirth;
        student.Gender = request.Gender;
        student.BloodGroup = request.BloodGroup;
        student.PermanentAddress = request.PermanentAddress;
        student.CurrentAddress = request.CurrentAddress;
        student.Category = request.Category;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
