using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.NAAC.Application.Commands;

public record CreateDvvQueryCommand(
    Guid TenantId,
    Guid SsrId,
    string QueryNumber,
    string CriterionNumber,
    string IndicatorNumber,
    string QueryText,
    DateTime ReceivedAt) : IRequest<Result<Guid>>;

public class CreateDvvQueryHandler : IRequestHandler<CreateDvvQueryCommand, Result<Guid>>
{
    private readonly INaacDbContext _db;

    public CreateDvvQueryHandler(INaacDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateDvvQueryCommand request, CancellationToken cancellationToken)
    {
        var query = new DvvQuery
        {
            TenantId = request.TenantId,
            SsrId = request.SsrId,
            QueryNumber = request.QueryNumber,
            CriterionNumber = request.CriterionNumber,
            IndicatorNumber = request.IndicatorNumber,
            QueryText = request.QueryText,
            Status = DvvStatus.Received,
            ReceivedAt = request.ReceivedAt
        };

        _db.DvvQueries.Add(query);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(query.Id);
    }
}
