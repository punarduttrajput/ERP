using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record CreateEmployeeCommand(
    Guid TenantId,
    Guid DepartmentId,
    string Designation,
    string EmploymentType,
    string FirstName,
    string LastName,
    string Email,
    string? MobileNumber,
    DateOnly DateOfBirth,
    string Gender,
    string? PanNumber,
    string? AadharNumber,
    DateOnly JoiningDate,
    Guid? ReportingManagerId
) : IRequest<Result<Guid>>;

public class CreateEmployeeHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;

    public CreateEmployeeHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var exists = await _db.Employees
            .AnyAsync(e => e.TenantId == request.TenantId && e.Email == request.Email, cancellationToken);

        if (exists)
            return Result.Failure<Guid>($"An employee with email '{request.Email}' already exists.");

        var seq = await _db.Employees.CountAsync(e => e.TenantId == request.TenantId, cancellationToken) + 1;
        var code = $"EMP-{seq:D5}";

        var employee = new Employee
        {
            TenantId = request.TenantId,
            EmployeeCode = code,
            DepartmentId = request.DepartmentId,
            Designation = request.Designation,
            EmploymentType = request.EmploymentType,
            Status = EmploymentStatus.Active,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            MobileNumber = request.MobileNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PanNumber = request.PanNumber,
            AadharNumber = request.AadharNumber,
            JoiningDate = request.JoiningDate,
            ReportingManagerId = request.ReportingManagerId
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(employee.Id);
    }
}
