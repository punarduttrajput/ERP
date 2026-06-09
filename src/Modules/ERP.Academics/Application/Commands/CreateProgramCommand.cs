using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateProgramCommand(
    Guid DepartmentId,
    string Code,
    string Name,
    int DurationYears,
    string DegreeType) : IRequest<Result<Guid>>;

public class CreateProgramHandler : IRequestHandler<CreateProgramCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateProgramHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateProgramCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var departmentExists = await _db.Departments
            .AnyAsync(x => x.Id == request.DepartmentId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (!departmentExists)
            return Result.Failure<Guid>("Department not found.");

        var duplicate = await _db.AcademicPrograms
            .AnyAsync(x => x.TenantId == tenantId && x.Code == request.Code && !x.IsDeleted, cancellationToken);

        if (duplicate)
            return Result.Failure<Guid>($"Program with code '{request.Code}' already exists.");

        var program = new AcademicProgram
        {
            TenantId = tenantId,
            DepartmentId = request.DepartmentId,
            Code = request.Code,
            Name = request.Name,
            DurationYears = request.DurationYears,
            TotalSemesters = request.DurationYears * 2,
            DegreeType = request.DegreeType
        };

        _db.AcademicPrograms.Add(program);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(program.Id);
    }
}
