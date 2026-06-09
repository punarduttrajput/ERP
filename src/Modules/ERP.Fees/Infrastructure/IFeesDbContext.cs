using ERP.Fees.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Fees.Infrastructure;

public interface IFeesDbContext
{
    DbSet<FeeStructure> FeeStructures { get; }
    DbSet<FeeComponent> FeeComponents { get; }
    DbSet<InstallmentSchedule> InstallmentSchedules { get; }
    DbSet<StudentFeeAccount> StudentFeeAccounts { get; }
    DbSet<FeeInstallment> FeeInstallments { get; }
    DbSet<FeePayment> FeePayments { get; }
    DbSet<Scholarship> Scholarships { get; }
    DbSet<StudentScholarship> StudentScholarships { get; }
    DbSet<RefundRequest> RefundRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
