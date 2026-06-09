using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Queries;

public record GetPayrollRunQuery(Guid PayrollRunId, Guid TenantId) : IRequest<Result<PayrollRunDto>>;

public record PayrollEntryDto(
    Guid Id,
    string EmployeeCode,
    string EmployeeName,
    decimal GrossPay,
    decimal PfEmployee,
    decimal PfEmployer,
    decimal? EsiEmployee,
    decimal? EsiEmployer,
    decimal TdsAmount,
    decimal TotalDeductions,
    decimal NetPay,
    string TaxRegime,
    bool PayslipGenerated
);

public record PayrollRunDto(
    Guid Id,
    int Month,
    int Year,
    bool IsPostedToGl,
    DateTime ProcessedAt,
    decimal TotalGrossPay,
    decimal TotalDeductions,
    decimal TotalNetPay,
    IReadOnlyList<PayrollEntryDto> Entries
);

public class GetPayrollRunHandler : IRequestHandler<GetPayrollRunQuery, Result<PayrollRunDto>>
{
    private readonly IHrmsDbContext _db;

    public GetPayrollRunHandler(IHrmsDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PayrollRunDto>> Handle(GetPayrollRunQuery request, CancellationToken cancellationToken)
    {
        var run = await _db.PayrollRuns
            .Include(r => r.Entries)
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRunId && r.TenantId == request.TenantId, cancellationToken);

        if (run is null)
            return Result.Failure<PayrollRunDto>("Payroll run not found.");

        return Result.Success(new PayrollRunDto(
            run.Id, run.Month, run.Year, run.IsPostedToGl,
            run.ProcessedAt, run.TotalGrossPay, run.TotalDeductions, run.TotalNetPay,
            run.Entries.Select(e => new PayrollEntryDto(
                e.Id, e.EmployeeCode, e.EmployeeName, e.GrossPay,
                e.PfEmployee, e.PfEmployer, e.EsiEmployee, e.EsiEmployer,
                e.TdsAmount, e.TotalDeductions, e.NetPay, e.TaxRegime, e.PayslipGenerated
            )).ToList()
        ));
    }
}
