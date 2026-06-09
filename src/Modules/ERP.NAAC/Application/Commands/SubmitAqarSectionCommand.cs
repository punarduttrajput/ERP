using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record SubmitAqarSectionCommand(Guid AqarId, Guid SectionId, string Content) : IRequest<Result>;

public class SubmitAqarSectionHandler : IRequestHandler<SubmitAqarSectionCommand, Result>
{
    private readonly INaacDbContext _db;

    public SubmitAqarSectionHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(SubmitAqarSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _db.AqarSections
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.AqarId == request.AqarId, cancellationToken);

        if (section is null)
            return Result.Failure("AQAR section not found.");

        if (section.Status != AqarStatus.InProgress)
            return Result.Failure("Section must be InProgress to submit.");

        section.Content = request.Content;
        section.Status = AqarStatus.UnderReview;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
