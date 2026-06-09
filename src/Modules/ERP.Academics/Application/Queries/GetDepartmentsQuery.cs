using ERP.Academics.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Queries;

public record DepartmentDto(Guid Id, string Code, string Name, Guid? HeadOfDepartmentUserId, bool IsActive);

public record GetDepartmentsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<DepartmentDto>>;

public class GetDepartmentsHandler : IRequestHandler<GetDepartmentsQuery, PagedResult<DepartmentDto>>
{
    private readonly IAcademicsDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public GetDepartmentsHandler(IAcademicsDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<PagedResult<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Departments
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new DepartmentDto(x.Id, x.Code, x.Name, x.HeadOfDepartmentUserId, x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<DepartmentDto>(items, total, request.Page, request.PageSize);
    }
}
