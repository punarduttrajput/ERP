using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record AssignAqarSectionCommand(Guid AqarId, Guid SectionId, Guid AssignedTo) : IRequest<Result>;

public class AssignAqarSectionHandler : IRequestHandler<AssignAqarSectionCommand, Result>
{
    private readonly INaacDbContext _db;

    public AssignAqarSectionHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(AssignAqarSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _db.AqarSections
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.AqarId == request.AqarId, cancellationToken);

        if (section is null)
            return Result.Failure("AQAR section not found.");

        section.AssignedTo = request.AssignedTo;
        section.Status = AqarStatus.InProgress;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
