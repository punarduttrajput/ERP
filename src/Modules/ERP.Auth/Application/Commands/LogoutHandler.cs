using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.Auth.Application.Commands;

public sealed class LogoutHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IAuthDbContext _db;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(IAuthDbContext db, ILogger<LogoutHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (token is null)
            return Result.Failure("Token not found.");

        if (!token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedReason = "Logout";
            token.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("User {UserId} logged out", token.UserId);
        return Result.Success();
    }
}
