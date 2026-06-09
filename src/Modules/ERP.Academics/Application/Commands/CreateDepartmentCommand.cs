using ERP.Academics.Domain;
using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Commands;

public record CreateDepartmentCommand(string Code, string Name, Guid? HeadOfDepartmentUserId) : IRequest<Result<Guid>>;

public class CreateDepartmentHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CreateDepartmentHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentTenant.TenantId ?? Guid.Empty;

        var duplicate = await _db.Departments
            .AnyAsync(x => x.TenantId == tenantId && x.Code == request.Code && !x.IsDeleted, cancellationToken);

        if (duplicate)
            return Result.Failure<Guid>($"Department with code '{request.Code}' already exists.");

        var department = new Department
        {
            TenantId = tenantId,
            Code = request.Code,
            Name = request.Name,
            HeadOfDepartmentUserId = request.HeadOfDepartmentUserId
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(department.Id);
    }
}
