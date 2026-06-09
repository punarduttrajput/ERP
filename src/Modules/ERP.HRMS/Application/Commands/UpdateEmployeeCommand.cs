using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record UpdateEmployeeCommand(
    Guid Id,
    Guid TenantId,
    string Designation,
    string EmploymentType,
    EmploymentStatus Status,
    string? MobileNumber,
    string? PanNumber,
    string? AadharNumber,
    DateOnly? ConfirmationDate,
    Guid? ReportingManagerId
) : IRequest<Result>;

public class UpdateEmployeeHandler : IRequestHandler<UpdateEmployeeCommand, Result>
{
    private readonly IHrmsDbContext _db;

    public UpdateEmployeeHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, cancellationToken);

        if (employee is null)
            return Result.Failure("Employee not found.");

        employee.Designation = request.Designation;
        employee.EmploymentType = request.EmploymentType;
        employee.Status = request.Status;
        employee.MobileNumber = request.MobileNumber;
        employee.PanNumber = request.PanNumber;
        employee.AadharNumber = request.AadharNumber;
        employee.ConfirmationDate = request.ConfirmationDate;
        employee.ReportingManagerId = request.ReportingManagerId;

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
