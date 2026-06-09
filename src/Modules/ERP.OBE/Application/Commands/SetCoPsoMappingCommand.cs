using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record CoPsoMappingItem(string CourseOutcomeCode, string PsoCode, int CorrelationLevel);

public record SetCoPsoMappingCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid ProgramId,
    IReadOnlyList<CoPsoMappingItem> Mappings) : IRequest<Result>;

public class SetCoPsoMappingHandler : IRequestHandler<SetCoPsoMappingCommand, Result>
{
    private readonly IObeDbContext _db;

    public SetCoPsoMappingHandler(IObeDbContext db) => _db = db;

    public async Task<Result> Handle(SetCoPsoMappingCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Mappings)
        {
            if (item.CorrelationLevel < 0 || item.CorrelationLevel > 3)
                return Result.Failure($"CorrelationLevel must be 0-3, got {item.CorrelationLevel} for {item.CourseOutcomeCode}->{item.PsoCode}.");
        }

        foreach (var item in request.Mappings)
        {
            var existing = await _db.CoPsoMappings.FirstOrDefaultAsync(
                x => x.TenantId == request.TenantId
                  && x.SubjectId == request.SubjectId
                  && x.CourseOutcomeCode == item.CourseOutcomeCode
                  && x.PsoCode == item.PsoCode,
                cancellationToken);

            if (existing is not null)
            {
                existing.CorrelationLevel = item.CorrelationLevel;
            }
            else
            {
                _db.CoPsoMappings.Add(new CoPsoMapping
                {
                    TenantId = request.TenantId,
                    SubjectId = request.SubjectId,
                    ProgramId = request.ProgramId,
                    CourseOutcomeCode = item.CourseOutcomeCode,
                    PsoCode = item.PsoCode,
                    CorrelationLevel = item.CorrelationLevel
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
