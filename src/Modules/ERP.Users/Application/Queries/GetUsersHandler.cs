using ERP.Shared.Application.Common;
using ERP.Users.API.Dtos;
using ERP.Users.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Users.Application.Queries;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, Result<PagedResult<UserResponseDto>>>
{
    private readonly IUsersDbContext _db;

    public GetUsersHandler(IUsersDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PagedResult<UserResponseDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // Join users (auth identity) with user_profiles (extended data)
        var query =
            from u in _db.Users.AsNoTracking()
            join p in _db.UserProfiles.AsNoTracking() on u.Id equals p.Id
            select new { u, p };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(x =>
                x.u.Email.ToLower().Contains(s)
                || (x.p.FirstName != null && x.p.FirstName.ToLower().Contains(s))
                || (x.p.LastName  != null && x.p.LastName.ToLower().Contains(s)));
        }

        if (request.IsActive.HasValue)
            query = query.Where(x => x.u.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page     = request.Page     < 1   ? 1   : request.Page;
        var pageSize = request.PageSize < 1   ? 20  :
                       request.PageSize > 100 ? 100 : request.PageSize;

        var users = await query
            .OrderByDescending(x => x.u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserResponseDto(
                x.u.Id,
                x.u.Email,
                x.p.FirstName,
                x.p.LastName,
                x.p.PhoneNumber,
                x.p.Department,
                x.p.JobTitle,
                x.p.AvatarUrl,
                x.u.IsActive,
                x.u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<UserResponseDto>>.Success(
            new PagedResult<UserResponseDto>(users, totalCount, page, pageSize));
    }
}
