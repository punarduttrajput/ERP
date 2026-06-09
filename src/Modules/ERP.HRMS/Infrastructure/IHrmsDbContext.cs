using ERP.HRMS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Infrastructure;

public interface IHrmsDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; }
    DbSet<RecruitmentRequisition> RecruitmentRequisitions { get; }
    DbSet<JobApplication> JobApplications { get; }
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveApplication> LeaveApplications { get; }
    DbSet<SalaryStructure> SalaryStructures { get; }
    DbSet<SalaryComponent> SalaryComponents { get; }
    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollEntry> PayrollEntries { get; }
    DbSet<Appraisal> Appraisals { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
