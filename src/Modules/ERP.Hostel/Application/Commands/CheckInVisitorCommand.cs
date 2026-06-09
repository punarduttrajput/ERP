using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.Hostel.Application.Commands;

public record CheckInVisitorCommand(
    string VisitorName,
    string VisitorMobile,
    string VisitorIdType,
    string VisitorIdNumber,
    Guid StudentId,
    string StudentName,
    Guid BlockId,
    string PurposeOfVisit,
    Guid CheckedInBy
) : IRequest<Result<Guid>>;

public class CheckInVisitorCommandHandler : IRequestHandler<CheckInVisitorCommand, Result<Guid>>
{
    private readonly IHostelDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public CheckInVisitorCommandHandler(IHostelDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(CheckInVisitorCommand request, CancellationToken cancellationToken)
    {
        var entry = new VisitorEntry
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            VisitorName = request.VisitorName,
            VisitorMobile = request.VisitorMobile,
            VisitorIdType = request.VisitorIdType,
            VisitorIdNumber = request.VisitorIdNumber,
            StudentId = request.StudentId,
            StudentName = request.StudentName,
            BlockId = request.BlockId,
            PurposeOfVisit = request.PurposeOfVisit,
            CheckInAt = DateTime.UtcNow,
            CheckOutAt = null,
            CheckedInBy = request.CheckedInBy
        };

        _db.VisitorEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entry.Id);
    }
}
