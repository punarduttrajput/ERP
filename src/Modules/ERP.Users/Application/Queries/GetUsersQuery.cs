using ERP.Shared.Application.Common;
using ERP.Users.API.Dtos;
using MediatR;

namespace ERP.Users.Application.Queries;

public record GetUsersQuery(int Page = 1, int PageSize = 20, string? Search = null, bool? IsActive = null)
    : IRequest<Result<PagedResult<UserResponseDto>>>;
