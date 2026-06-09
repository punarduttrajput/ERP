using ERP.Shared.Application.Common;
using ERP.Tenants.API.Dtos;
using MediatR;

namespace ERP.Tenants.Application.Queries;

public record GetTenantQuery(Guid TenantId) : IRequest<Result<TenantResponseDto>>;
