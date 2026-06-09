using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Queries;

public record GetFeeStructureQuery(Guid? ProgramId, int? SemesterNumber, int? AcademicYear)
    : IRequest<Result<IReadOnlyList<FeeStructure>>>;

public class GetFeeStructureHandler : IRequestHandler<GetFeeStructureQuery, Result<IReadOnlyList<FeeStructure>>>
{
    private readonly IFeesDbContext _db;

    public GetFeeStructureHandler(IFeesDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<FeeStructure>>> Handle(GetFeeStructureQuery request, CancellationToken cancellationToken)
    {
        var query = _db.FeeStructures.Include(s => s.Components).AsQueryable();

        if (request.ProgramId.HasValue)
            query = query.Where(s => s.ProgramId == request.ProgramId.Value);
        if (request.SemesterNumber.HasValue)
            query = query.Where(s => s.SemesterNumber == request.SemesterNumber.Value);
        if (request.AcademicYear.HasValue)
            query = query.Where(s => s.AcademicYear == request.AcademicYear.Value);

        var results = await query.ToListAsync(cancellationToken);
        return Result<IReadOnlyList<FeeStructure>>.Success(results);
    }
}
