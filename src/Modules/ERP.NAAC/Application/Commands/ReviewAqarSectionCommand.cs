using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record ReviewAqarSectionCommand(
    Guid AqarId,
    Guid SectionId,
    bool Approved,
    Guid ReviewedBy,
    string? Comment = null) : IRequest<Result>;

public class ReviewAqarSectionHandler : IRequestHandler<ReviewAqarSectionCommand, Result>
{
    private readonly INaacDbContext _db;

    public ReviewAqarSectionHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(ReviewAqarSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _db.AqarSections
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.AqarId == request.AqarId, cancellationToken);

        if (section is null)
            return Result.Failure("AQAR section not found.");

        if (section.Status != AqarStatus.UnderReview)
            return Result.Failure("Section must be UnderReview to review.");

        section.ReviewedBy = request.ReviewedBy;
        section.ReviewedAt = DateTime.UtcNow;
        section.ReviewComment = request.Comment;
        section.Status = request.Approved ? AqarStatus.Approved : AqarStatus.InProgress;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
