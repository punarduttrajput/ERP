using ERP.Attendance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ERP.Attendance.Application.Commands;

public record QrResponse(string Token, DateTime ExpiresAt);

public record GenerateQrCommand(Guid SessionId) : IRequest<Result<QrResponse>>;

public class GenerateQrHandler : IRequestHandler<GenerateQrCommand, Result<QrResponse>>
{
    private readonly IAttendanceDbContext _db;

    public GenerateQrHandler(IAttendanceDbContext db) => _db = db;

    public async Task<Result<QrResponse>> Handle(GenerateQrCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.AttendanceSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return Result<QrResponse>.Failure("Session not found.");

        if (session.IsLocked)
            return Result<QrResponse>.Failure("Session is locked.");

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var expiresAt = DateTime.UtcNow.AddMinutes(5);

        session.QrToken = token;
        session.QrExpiresAt = expiresAt;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<QrResponse>.Success(new QrResponse(token, expiresAt));
    }
}
