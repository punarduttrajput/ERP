using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.NAAC.Application.Commands;

public record RespondToDvvQueryCommand(
    Guid DvvQueryId,
    string Response,
    Guid RespondedBy,
    string[]? SupportingDocUrls = null) : IRequest<Result>;

public class RespondToDvvQueryHandler : IRequestHandler<RespondToDvvQueryCommand, Result>
{
    private readonly INaacDbContext _db;

    public RespondToDvvQueryHandler(INaacDbContext db) => _db = db;

    public async Task<Result> Handle(RespondToDvvQueryCommand request, CancellationToken cancellationToken)
    {
        var query = await _db.DvvQueries
            .FirstOrDefaultAsync(q => q.Id == request.DvvQueryId, cancellationToken);

        if (query is null)
            return Result.Failure("DVV query not found.");

        query.Response = request.Response;
        query.Status = DvvStatus.Responded;
        query.RespondedAt = DateTime.UtcNow;
        query.RespondedBy = request.RespondedBy;

        if (request.SupportingDocUrls is not null && request.SupportingDocUrls.Length > 0)
            query.SupportingDocUrls = System.Text.Json.JsonSerializer.Serialize(request.SupportingDocUrls);

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
