using ERP.OBE.Domain;
using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Commands;

public record CoPoMappingItem(string CourseOutcomeCode, string ProgramOutcomeCode, int CorrelationLevel);

public record SetCoPoMappingCommand(
    Guid TenantId,
    Guid SubjectId,
    Guid ProgramId,
    IReadOnlyList<CoPoMappingItem> Mappings) : IRequest<Result>;

public class SetCoPoMappingHandler : IRequestHandler<SetCoPoMappingCommand, Result>
{
    private readonly IObeDbContext _db;

    public SetCoPoMappingHandler(IObeDbContext db) => _db = db;

    public async Task<Result> Handle(SetCoPoMappingCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Mappings)
        {
            if (item.CorrelationLevel < 0 || item.CorrelationLevel > 3)
                return Result.Failure($"CorrelationLevel must be 0-3, got {item.CorrelationLevel} for {item.CourseOutcomeCode}->{item.ProgramOutcomeCode}.");
        }

        foreach (var item in request.Mappings)
        {
            var existing = await _db.CoPoMappings.FirstOrDefaultAsync(
                x => x.TenantId == request.TenantId
                  && x.SubjectId == request.SubjectId
                  && x.CourseOutcomeCode == item.CourseOutcomeCode
                  && x.ProgramOutcomeCode == item.ProgramOutcomeCode,
                cancellationToken);

            if (existing is not null)
            {
                existing.CorrelationLevel = item.CorrelationLevel;
            }
            else
            {
                _db.CoPoMappings.Add(new CoPoMapping
                {
                    TenantId = request.TenantId,
                    SubjectId = request.SubjectId,
                    ProgramId = request.ProgramId,
                    CourseOutcomeCode = item.CourseOutcomeCode,
                    ProgramOutcomeCode = item.ProgramOutcomeCode,
                    CorrelationLevel = item.CorrelationLevel
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
