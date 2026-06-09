using ERP.NAAC.Domain;
using ERP.NAAC.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;

namespace ERP.NAAC.Application.Commands;

public record CreateAqarCommand(Guid TenantId, int AcademicYear, string Title) : IRequest<Result<Guid>>;

public class CreateAqarHandler : IRequestHandler<CreateAqarCommand, Result<Guid>>
{
    private readonly INaacDbContext _db;

    public CreateAqarHandler(INaacDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateAqarCommand request, CancellationToken cancellationToken)
    {
        var aqar = new AqarReport
        {
            TenantId = request.TenantId,
            AcademicYear = request.AcademicYear,
            Title = request.Title,
            Status = AqarStatus.Draft
        };

        foreach (var criterion in NaacCriteria.All)
        {
            aqar.Sections.Add(new AqarSection
            {
                TenantId = request.TenantId,
                AqarId = aqar.Id,
                CriterionNumber = criterion.Number,
                Title = criterion.Title,
                Status = AqarStatus.Draft
            });
        }

        _db.AqarReports.Add(aqar);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(aqar.Id);
    }
}
