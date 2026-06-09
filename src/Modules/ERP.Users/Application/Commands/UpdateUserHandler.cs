using ERP.Shared.Application.Common;
using ERP.Users.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Users.Application.Commands;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUsersDbContext _db;

    public UpdateUserHandler(IUsersDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.Id == request.UserId, cancellationToken);

        if (profile is null)
            return Result.Failure("User not found.");

        // Profile-only fields — auth fields (email, password) are managed by the Auth module
        if (request.FirstName  is not null) profile.FirstName  = request.FirstName;
        if (request.LastName   is not null) profile.LastName   = request.LastName;
        if (request.PhoneNumber is not null) profile.PhoneNumber = request.PhoneNumber;
        if (request.Department is not null) profile.Department = request.Department;
        if (request.JobTitle   is not null) profile.JobTitle   = request.JobTitle;
        if (request.AvatarUrl  is not null) profile.AvatarUrl  = request.AvatarUrl;
        profile.UpdatedAt = DateTime.UtcNow;

        // Sync display name back to the identity record so JWT claims stay fresh
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is not null)
        {
            if (request.FirstName is not null) user.FirstName = request.FirstName;
            if (request.LastName  is not null) user.LastName  = request.LastName;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
