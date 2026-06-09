using ERP.Shared.Application.Common;
using ERP.Users.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Users.Application.Commands;

public sealed class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUsersDbContext _db;

    public DeactivateUserHandler(IUsersDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        // IsActive lives on the auth User record
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("User not found.");

        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
