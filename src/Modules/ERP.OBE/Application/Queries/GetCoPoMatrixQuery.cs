using ERP.OBE.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.OBE.Application.Queries;

public record GetCoPoMatrixQuery(Guid TenantId, Guid ProgramId) : IRequest<Result<CoPoMatrixDto>>;

public record CoPoMappingDto(Guid SubjectId, string CourseOutcomeCode, string ProgramOutcomeCode, int CorrelationLevel);
public record CoPoMatrixDto(Guid ProgramId, IReadOnlyList<CoPoMappingDto> Mappings);

public class GetCoPoMatrixHandler : IRequestHandler<GetCoPoMatrixQuery, Result<CoPoMatrixDto>>
{
    private readonly IObeDbContext _db;

    public GetCoPoMatrixHandler(IObeDbContext db) => _db = db;

    public async Task<Result<CoPoMatrixDto>> Handle(GetCoPoMatrixQuery request, CancellationToken cancellationToken)
    {
        var mappings = await _db.CoPoMappings
            .Where(x => x.TenantId == request.TenantId && x.ProgramId == request.ProgramId && !x.IsDeleted)
            .Select(x => new CoPoMappingDto(x.SubjectId, x.CourseOutcomeCode, x.ProgramOutcomeCode, x.CorrelationLevel))
            .ToListAsync(cancellationToken);

        return Result<CoPoMatrixDto>.Success(new CoPoMatrixDto(request.ProgramId, mappings));
    }
}
