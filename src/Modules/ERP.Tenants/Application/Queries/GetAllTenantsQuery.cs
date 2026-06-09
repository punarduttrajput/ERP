using ERP.Shared.Application.Common;
using ERP.Tenants.API.Dtos;
using MediatR;

namespace ERP.Tenants.Application.Queries;

public record GetAllTenantsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null
) : IRequest<Result<PagedResult<TenantResponseDto>>>;
