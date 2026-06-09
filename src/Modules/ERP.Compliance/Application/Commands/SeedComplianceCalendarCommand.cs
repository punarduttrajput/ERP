using ERP.Compliance.Domain;
using ERP.Compliance.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Compliance.Application.Commands;

public record SeedComplianceCalendarCommand(Guid TenantId, int AcademicYear) : IRequest<Result<int>>;

public class SeedComplianceCalendarHandler : IRequestHandler<SeedComplianceCalendarCommand, Result<int>>
{
    private readonly IComplianceDbContext _db;

    public SeedComplianceCalendarHandler(IComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(SeedComplianceCalendarCommand request, CancellationToken cancellationToken)
    {
        var defaults = new[]
        {
            (Authority: ComplianceAuthority.UGC,   Title: "UGC Annual Report",                       Month: 3,  Day: 31),
            (Authority: ComplianceAuthority.AISHE,  Title: "AISHE Annual Return",                     Month: 12, Day: 31),
            (Authority: ComplianceAuthority.AICTE,  Title: "AICTE Annual Disclosure",                 Month: 4,  Day: 30),
            (Authority: ComplianceAuthority.NAAC,   Title: "NAAC Annual Quality Assurance Report",    Month: 6,  Day: 30),
        };

        int seeded = 0;
        foreach (var (authority, title, month, day) in defaults)
        {
            // Idempotency guard: skip if already present for this tenant/authority/year/title
            var exists = await _db.ComplianceItems.AnyAsync(
                x => x.TenantId == request.TenantId
                     && x.Authority == authority
                     && x.AcademicYear == request.AcademicYear
                     && x.Title == title
                     && !x.IsDeleted,
                cancellationToken);

            if (exists)
                continue;

            _db.ComplianceItems.Add(new ComplianceItem
            {
                TenantId = request.TenantId,
                Authority = authority,
                Title = title,
                DueDate = new DateOnly(request.AcademicYear, month, day),
                AcademicYear = request.AcademicYear,
                Status = ComplianceStatus.Pending,
                IsRecurring = true,
                RecurrencePattern = "Annual"
            });
            seeded++;
        }

        if (seeded > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(seeded);
    }
}
