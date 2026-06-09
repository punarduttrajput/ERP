using ERP.Hostel.Domain;
using ERP.Hostel.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Hostel.Application.Queries;

public record BlockSummaryDto(
    Guid Id,
    string Name,
    string Gender,
    int TotalRooms,
    int OccupiedRooms,
    bool IsActive
);

public record GetBlocksQuery : IRequest<Result<IReadOnlyList<BlockSummaryDto>>>;

public class GetBlocksQueryHandler : IRequestHandler<GetBlocksQuery, Result<IReadOnlyList<BlockSummaryDto>>>
{
    private readonly IHostelDbContext _db;

    public GetBlocksQueryHandler(IHostelDbContext db)
    {
        _db = db;
    }

    public async Task<Result<IReadOnlyList<BlockSummaryDto>>> Handle(GetBlocksQuery request, CancellationToken cancellationToken)
    {
        var blocks = await _db.HostelBlocks
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BlockSummaryDto(b.Id, b.Name, b.Gender, b.TotalRooms, b.OccupiedRooms, b.IsActive))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BlockSummaryDto>>.Success(blocks);
    }
}
