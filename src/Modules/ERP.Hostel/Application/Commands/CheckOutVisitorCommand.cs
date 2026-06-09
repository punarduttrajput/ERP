using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Commands;

public record CheckOutVisitorCommand(Guid VisitorEntryId) : IRequest<Result>;

public class CheckOutVisitorCommandHandler : IRequestHandler<CheckOutVisitorCommand, Result>
{
    private readonly IHostelDbContext _db;

    public CheckOutVisitorCommandHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(CheckOutVisitorCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.VisitorEntries
            .FirstOrDefaultAsync(v => v.Id == request.VisitorEntryId && v.CheckOutAt == null, cancellationToken);

        if (entry is null)
            return Result.Failure("Visitor entry not found or already checked out.");

        entry.CheckOutAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
