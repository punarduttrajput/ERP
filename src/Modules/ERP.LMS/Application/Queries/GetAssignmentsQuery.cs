using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record AssignmentDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string Description,
    DateTime DueDate,
    decimal MaxMarks,
    bool IsVisible,
    Guid CreatedBy,
    DateTime CreatedAt);

public record GetAssignmentsQuery(Guid SubjectId, Guid BatchId) : IRequest<Result<IReadOnlyList<AssignmentDto>>>;

public class GetAssignmentsHandler : IRequestHandler<GetAssignmentsQuery, Result<IReadOnlyList<AssignmentDto>>>
{
    private readonly ILmsDbContext _db;

    public GetAssignmentsHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<AssignmentDto>>> Handle(GetAssignmentsQuery query, CancellationToken ct)
    {
        var items = await _db.Assignments
            .Where(a => a.SubjectId == query.SubjectId && a.BatchId == query.BatchId && !a.IsDeleted)
            .OrderBy(a => a.DueDate)
            .Select(a => new AssignmentDto(a.Id, a.SubjectId, a.BatchId, a.Title, a.Description, a.DueDate, a.MaxMarks, a.IsVisible, a.AssignmentCreatedBy, a.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<AssignmentDto>>(items);
    }
}
