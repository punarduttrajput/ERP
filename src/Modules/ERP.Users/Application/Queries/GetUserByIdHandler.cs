using ERP.Shared.Application.Common;
using ERP.Users.API.Dtos;
using ERP.Users.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Users.Application.Queries;

public sealed class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<UserResponseDto>>
{
    private readonly IUsersDbContext _db;

    public GetUserByIdHandler(IUsersDbContext db)
    {
        _db = db;
    }

    public async Task<Result<UserResponseDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await (
            from u in _db.Users.AsNoTracking()
            join p in _db.UserProfiles.AsNoTracking() on u.Id equals p.Id
            where u.Id == request.UserId
            select new UserResponseDto(
                u.Id,
                u.Email,
                p.FirstName,
                p.LastName,
                p.PhoneNumber,
                p.Department,
                p.JobTitle,
                p.AvatarUrl,
                u.IsActive,
                u.CreatedAt)
        ).FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return Result<UserResponseDto>.Failure("User not found.");

        return Result<UserResponseDto>.Success(result);
    }
}
