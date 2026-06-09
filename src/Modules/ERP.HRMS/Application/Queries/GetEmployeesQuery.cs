using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Queries;

public record GetEmployeesQuery(
    Guid TenantId,
    Guid? DepartmentId,
    EmploymentStatus? Status,
    int Page,
    int PageSize
) : IRequest<Result<PagedEmployeesDto>>;

public record PagedEmployeesDto(IReadOnlyList<EmployeeDto> Items, int Total);

public class GetEmployeesHandler : IRequestHandler<GetEmployeesQuery, Result<PagedEmployeesDto>>
{
    private readonly IHrmsDbContext _db;

    public GetEmployeesHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedEmployeesDto>> Handle(GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Employees
            .Include(e => e.Documents)
            .Where(e => e.TenantId == request.TenantId);

        if (request.DepartmentId.HasValue)
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.EmployeeCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedEmployeesDto(
            items.Select(GetEmployeeHandler.MapToDto).ToList(),
            total
        ));
    }
}
