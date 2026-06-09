using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Application.Commands;

public record ApplyScholarshipManualCommand(
    Guid StudentId,
    Guid ScholarshipId,
    int AcademicYear,
    Guid AppliedBy
) : IRequest<Result>;

public class ApplyScholarshipManualHandler : IRequestHandler<ApplyScholarshipManualCommand, Result>
{
    private readonly IFeesDbContext _db;
    private readonly ICurrentTenant _currentTenant;

    public ApplyScholarshipManualHandler(IFeesDbContext db, ICurrentTenant currentTenant)
    {
        _db = db;
        _currentTenant = currentTenant;
    }

    public async Task<Result> Handle(ApplyScholarshipManualCommand request, CancellationToken cancellationToken)
    {
        var scholarship = await _db.Scholarships.FirstOrDefaultAsync(s => s.Id == request.ScholarshipId, cancellationToken);
        if (scholarship is null || !scholarship.IsActive)
            return Result.Failure("Scholarship not found or inactive.");

        var account = await _db.StudentFeeAccounts
            .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && a.AcademicYear == request.AcademicYear, cancellationToken);
        if (account is null)
            return Result.Failure("Student fee account not found.");

        var alreadyApplied = await _db.StudentScholarships
            .AnyAsync(ss => ss.StudentId == request.StudentId && ss.ScholarshipId == request.ScholarshipId && ss.AcademicYear == request.AcademicYear, cancellationToken);
        if (alreadyApplied)
            return Result.Failure("Scholarship already applied for this student and academic year.");

        decimal discount;
        if (scholarship.DiscountAmount.HasValue)
            discount = scholarship.DiscountAmount.Value;
        else if (scholarship.DiscountPercent.HasValue)
            discount = account.TotalAmount * scholarship.DiscountPercent.Value / 100;
        else
            return Result.Failure("Scholarship has no discount configured.");

        var studentScholarship = new StudentScholarship
        {
            TenantId = _currentTenant.TenantId ?? Guid.Empty,
            StudentId = request.StudentId,
            ScholarshipId = request.ScholarshipId,
            AcademicYear = request.AcademicYear,
            DiscountApplied = discount,
            AppliedBy = request.AppliedBy,
            AppliedAt = DateTime.UtcNow
        };

        _db.StudentScholarships.Add(studentScholarship);

        account.DiscountAmount += discount;
        account.NetAmount = account.TotalAmount - account.DiscountAmount;
        account.DueAmount = account.NetAmount - account.PaidAmount;

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
