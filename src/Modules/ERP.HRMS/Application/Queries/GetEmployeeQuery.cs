using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Queries;

public record GetEmployeeQuery(Guid Id, Guid TenantId) : IRequest<Result<EmployeeDto>>;

public record EmployeeDocumentDto(Guid Id, string DocumentType, string FileName, string BlobUrl);

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    Guid DepartmentId,
    string Designation,
    string EmploymentType,
    EmploymentStatus Status,
    string FirstName,
    string LastName,
    string Email,
    string? MobileNumber,
    DateOnly DateOfBirth,
    string Gender,
    string? PanNumber,
    DateOnly JoiningDate,
    DateOnly? ConfirmationDate,
    Guid? ReportingManagerId,
    Guid? UserId,
    IReadOnlyList<EmployeeDocumentDto> Documents
);

public class GetEmployeeHandler : IRequestHandler<GetEmployeeQuery, Result<EmployeeDto>>
{
    private readonly IHrmsDbContext _db;

    public GetEmployeeHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<EmployeeDto>> Handle(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        var employee = await _db.Employees
            .Include(e => e.Documents)
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TenantId == request.TenantId, cancellationToken);

        if (employee is null)
            return Result.Failure<EmployeeDto>("Employee not found.");

        return Result.Success(MapToDto(employee));
    }

    internal static EmployeeDto MapToDto(Employee e) => new(
        e.Id, e.EmployeeCode, e.DepartmentId, e.Designation, e.EmploymentType,
        e.Status, e.FirstName, e.LastName, e.Email, e.MobileNumber,
        e.DateOfBirth, e.Gender, e.PanNumber, e.JoiningDate, e.ConfirmationDate,
        e.ReportingManagerId, e.UserId,
        e.Documents.Select(d => new EmployeeDocumentDto(d.Id, d.DocumentType, d.FileName, d.BlobUrl)).ToList()
    );
}
