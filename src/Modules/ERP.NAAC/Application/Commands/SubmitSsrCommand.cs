using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record SubmitSsrCommand(Guid SsrId) : IRequest<Result>;

public class SubmitSsrHandler : IRequestHandler<SubmitSsrCommand, Result>
{
    private readonly INaacDbContext _db;

    public SubmitSsrHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(SubmitSsrCommand request, CancellationToken cancellationToken)
    {
        var ssr = await _db.SsrReports
            .FirstOrDefaultAsync(r => r.Id == request.SsrId, cancellationToken);

        if (ssr is null)
            return Result.Failure("SSR not found.");

        if (ssr.Status == SsrStatus.Submitted)
            return Result.Failure("SSR is already submitted.");

        ssr.Status = SsrStatus.Submitted;
        ssr.SubmittedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
