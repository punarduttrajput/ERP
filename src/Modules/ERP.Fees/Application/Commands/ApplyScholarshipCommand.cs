using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Commands;

public record ApplyScholarshipCommand(
    Guid StudentId,
    int AcademicYear,
    decimal? MeritScore,
    string Category
) : IRequest<Result>;

public class ApplyScholarshipHandler : IRequestHandler<ApplyScholarshipCommand, Result>
{
    private readonly IFeesDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public ApplyScholarshipHandler(IFeesDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(ApplyScholarshipCommand request, CancellationToken cancellationToken)
    {
        var scholarships = await _db.Scholarships
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);

        var account = await _db.StudentFeeAccounts
            .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && a.AcademicYear == request.AcademicYear, cancellationToken);

        if (account is null)
            return Result.Failure("Student fee account not found.");

        var alreadyApplied = await _db.StudentScholarships
            .Where(ss => ss.StudentId == request.StudentId && ss.AcademicYear == request.AcademicYear)
            .Select(ss => ss.ScholarshipId)
            .ToListAsync(cancellationToken);

        foreach (var scholarship in scholarships)
        {
            if (alreadyApplied.Contains(scholarship.Id))
                continue;

            var eligible = scholarship.ScholarshipType switch
            {
                ScholarshipType.MeritBased => request.MeritScore.HasValue && scholarship.MinMeritScore.HasValue
                    && request.MeritScore.Value >= scholarship.MinMeritScore.Value,
                ScholarshipType.CategoryBased => scholarship.EligibleCategories != null
                    && scholarship.EligibleCategories.Split(',', StringSplitOptions.TrimEntries).Contains(request.Category),
                ScholarshipType.NeedBased => true,
                _ => false
            };

            if (!eligible)
                continue;

            decimal discount;
            if (scholarship.DiscountAmount.HasValue)
                discount = scholarship.DiscountAmount.Value;
            else if (scholarship.DiscountPercent.HasValue)
                discount = account.TotalAmount * scholarship.DiscountPercent.Value / 100;
            else
                continue;

            var studentScholarship = new StudentScholarship
            {
                TenantId = _currentTenant.TenantId ?? Guid.Empty,
                StudentId = request.StudentId,
                ScholarshipId = scholarship.Id,
                AcademicYear = request.AcademicYear,
                DiscountApplied = discount,
                AppliedAt = DateTime.UtcNow
            };

            _db.StudentScholarships.Add(studentScholarship);

            account.DiscountAmount += discount;
            account.NetAmount = account.TotalAmount - account.DiscountAmount;
            account.DueAmount = account.NetAmount - account.PaidAmount;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
