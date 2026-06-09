using ERP.Academics.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Academics.Application.Queries;

public record ProgramOutcomeDto(Guid Id, string Code, string Description);
public record ProgramSpecificOutcomeDto(Guid Id, string Code, string Description);
public record ProgramOutcomesResult(IReadOnlyList<ProgramOutcomeDto> POs, IReadOnlyList<ProgramSpecificOutcomeDto> PSOs);

public record GetProgramOutcomesQuery(Guid ProgramId) : IRequest<ProgramOutcomesResult>;

public class GetProgramOutcomesHandler : IRequestHandler<GetProgramOutcomesQuery, ProgramOutcomesResult>
{
    private readonly IAcademicsDbContext _db;

    public GetProgramOutcomesHandler(IAcademicsDbContext db) => _db = db;

    public async Task<ProgramOutcomesResult> Handle(GetProgramOutcomesQuery request, CancellationToken cancellationToken)
    {
        var pos = await _db.ProgramOutcomes
            .Where(x => x.ProgramId == request.ProgramId && !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new ProgramOutcomeDto(x.Id, x.Code, x.Description))
            .ToListAsync(cancellationToken);

        var psos = await _db.ProgramSpecificOutcomes
            .Where(x => x.ProgramId == request.ProgramId && !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new ProgramSpecificOutcomeDto(x.Id, x.Code, x.Description))
            .ToListAsync(cancellationToken);

        return new ProgramOutcomesResult(pos, psos);
    }
}
