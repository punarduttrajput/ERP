using ERP.Reporting.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.Reporting.Infrastructure;

public interface IReportingDbContext
{
    DbSet<ReportDefinition> ReportDefinitions { get; }
    DbSet<ReportColumn> ReportColumns { get; }
    DbSet<ReportFilter> ReportFilters { get; }
    DbSet<ReportSchedule> ReportSchedules { get; }
    DbSet<ReportExecution> ReportExecutions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
