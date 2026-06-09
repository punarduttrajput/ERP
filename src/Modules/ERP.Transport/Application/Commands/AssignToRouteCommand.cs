using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using ERP.Transport.Domain;
using ERP.Transport.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Transport.Application.Commands;

public record AssignToRouteCommand(
    Guid RouteId,
    Guid StopId,
    Guid MemberId,
    string MemberType,
    string MemberName,
    int AcademicYear
) : IRequest<Result<Guid>>;

public class AssignToRouteCommandHandler : IRequestHandler<AssignToRouteCommand, Result<Guid>>
{
    private readonly ITransportDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public AssignToRouteCommandHandler(ITransportDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result<Guid>> Handle(AssignToRouteCommand request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId, cancellationToken);
        if (route is null)
            return Result<Guid>.Failure("Route not found.");

        var stop = await _db.RouteStops.FirstOrDefaultAsync(
            s => s.Id == request.StopId && s.RouteId == request.RouteId, cancellationToken);
        if (stop is null)
            return Result<Guid>.Failure("Stop not found on this route.");

        var alreadyAssigned = await _db.RouteAssignments.AnyAsync(
            a => a.RouteId == request.RouteId && a.MemberId == request.MemberId && a.AcademicYear == request.AcademicYear,
            cancellationToken);
        if (alreadyAssigned)
            return Result<Guid>.Failure("Member is already assigned to this route for the given academic year.");

        var assignment = new RouteAssignment
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            RouteId = request.RouteId,
            StopId = request.StopId,
            MemberId = request.MemberId,
            MemberType = request.MemberType,
            MemberName = request.MemberName,
            AcademicYear = request.AcademicYear
        };

        _db.RouteAssignments.Add(assignment);

        route.TotalPassengers += 1;

        await _db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(assignment.Id);
    }
}
