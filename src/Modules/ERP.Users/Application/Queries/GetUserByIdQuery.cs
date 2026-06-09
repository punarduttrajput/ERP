using ERP.Shared.Application.Common;
using ERP.Users.API.Dtos;
using MediatR;

namespace ERP.Users.Application.Queries;

public record GetUserByIdQuery(Guid UserId) : IRequest<Result<UserResponseDto>>;
