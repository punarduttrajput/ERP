using ERP.LMS.Domain;
using ERP.LMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Application.Queries;

public record CourseContentDto(
    Guid Id,
    Guid SubjectId,
    Guid BatchId,
    string Title,
    string? Description,
    ContentType ContentType,
    string? BlobUrl,
    string? ExternalUrl,
    int OrderIndex,
    bool IsVisible,
    Guid UploadedBy,
    DateTime CreatedAt);

public record GetCourseContentQuery(Guid SubjectId, Guid BatchId) : IRequest<Result<IReadOnlyList<CourseContentDto>>>;

public class GetCourseContentHandler : IRequestHandler<GetCourseContentQuery, Result<IReadOnlyList<CourseContentDto>>>
{
    private readonly ILmsDbContext _db;

    public GetCourseContentHandler(ILmsDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CourseContentDto>>> Handle(GetCourseContentQuery query, CancellationToken ct)
    {
        var items = await _db.CourseContents
            .Where(c => c.SubjectId == query.SubjectId && c.BatchId == query.BatchId && !c.IsDeleted)
            .OrderBy(c => c.OrderIndex)
            .Select(c => new CourseContentDto(c.Id, c.SubjectId, c.BatchId, c.Title, c.Description, c.ContentType, c.BlobUrl, c.ExternalUrl, c.OrderIndex, c.IsVisible, c.UploadedBy, c.CreatedAt))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<CourseContentDto>>(items);
    }
}
