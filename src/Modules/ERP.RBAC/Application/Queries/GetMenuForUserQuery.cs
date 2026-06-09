using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.RBAC.Application.Queries;

public record MenuItemDto(
    Guid Id,
    string Label,
    string? Icon,
    string? Route,
    int Order,
    List<MenuItemDto> Children
);

public record GetMenuForUserQuery(Guid UserId) : IRequest<Result<List<MenuItemDto>>>;
