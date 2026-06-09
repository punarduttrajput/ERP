using ERP.Auth.Application.Services;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Auth.Application.Commands;

public sealed class DisableMfaHandler : IRequestHandler<DisableMfaCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ITotpService _totp;

    public DisableMfaHandler(IAuthDbContext db, ITotpService totp)
    {
        _db = db;
        _totp = totp;
    }

    public async Task<Result> Handle(DisableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, cancellationToken);
        if (user is null)
            return Result.Failure("User not found.");

        if (!user.MfaEnabled)
            return Result.Failure("MFA is not enabled.");

        if (!_totp.Verify(user.MfaSecret!, request.TotpCode))
            return Result.Failure("Invalid TOTP code.");

        user.MfaEnabled = false;
        user.MfaSecret = null;
        user.MfaRecoveryCodes = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
