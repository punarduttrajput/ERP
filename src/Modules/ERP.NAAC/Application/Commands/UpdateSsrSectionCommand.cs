using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record UpdateSsrSectionCommand(Guid SsrId, Guid SectionId, string Content, Guid EditedBy) : IRequest<Result>;

public class UpdateSsrSectionHandler : IRequestHandler<UpdateSsrSectionCommand, Result>
{
    private readonly INaacDbContext _db;

    public UpdateSsrSectionHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateSsrSectionCommand request, CancellationToken cancellationToken)
    {
        var section = await _db.SsrSections
            .FirstOrDefaultAsync(s => s.Id == request.SectionId && s.SsrId == request.SsrId, cancellationToken);

        if (section is null)
            return Result.Failure("Section not found.");

        section.Content = request.Content;
        section.LastEditedBy = request.EditedBy;
        section.LastEditedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
