using ERP.HRMS.Domain;
using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record RunPayrollCommand(
    Guid TenantId,
    int Month,
    int Year,
    Guid ProcessedBy
) : IRequest<Result<Guid>>;

public class RunPayrollHandler : IRequestHandler<RunPayrollCommand, Result<Guid>>
{
    private readonly IHrmsDbContext _db;
    private readonly PayrollCalculatorService _calculator;

    public RunPayrollHandler(IHrmsDbContext db, PayrollCalculatorService calculator)
    {
        _db = db;
        _calculator = calculator;
    }

    public async Task<Result<Guid>> Handle(RunPayrollCommand request, CancellationToken cancellationToken)
    {
        var alreadyRun = await _db.PayrollRuns
            .AnyAsync(r => r.TenantId == request.TenantId && r.Month == request.Month && r.Year == request.Year, cancellationToken);

        if (alreadyRun)
            return Result.Failure<Guid>($"Payroll for {request.Month}/{request.Year} has already been processed.");

        var employees = await _db.Employees
            .Where(e => e.TenantId == request.TenantId && e.Status == EmploymentStatus.Active)
            .ToListAsync(cancellationToken);

        if (!employees.Any())
            return Result.Failure<Guid>("No active employees found.");

        var structureIds = employees
            .Select(e => e.Id)
            .ToList();

        // Load all active salary structures with components for this tenant
        var structures = await _db.SalaryStructures
            .Include(s => s.Components)
            .Where(s => s.TenantId == request.TenantId && s.IsActive)
            .ToListAsync(cancellationToken);

        var run = new PayrollRun
        {
            TenantId = request.TenantId,
            Month = request.Month,
            Year = request.Year,
            ProcessedAt = DateTime.UtcNow,
            ProcessedBy = request.ProcessedBy
        };

        // Default: use the first active structure if employee has no explicit assignment
        var defaultStructure = structures.FirstOrDefault();

        foreach (var employee in employees)
        {
            var structure = defaultStructure;
            if (structure is null) continue;

            var components = structure.Components.ToList();

            // Calculate basic pay
            var basicComponent = components.FirstOrDefault(c =>
                c.Name.Equals("Basic", StringComparison.OrdinalIgnoreCase) &&
                c.ComponentType == ComponentType.Earning);

            var basicPay = basicComponent?.IsPercentage == false
                ? basicComponent.Amount ?? 0m
                : 0m;

            // Calculate all earning components to arrive at gross
            decimal grossPay = 0m;
            foreach (var comp in components.Where(c => c.ComponentType == ComponentType.Earning))
            {
                if (!comp.IsPercentage)
                {
                    grossPay += comp.Amount ?? 0m;
                }
                else
                {
                    // Apply percentage to base component value
                    decimal baseValue = comp.BaseComponent?.Equals("Basic", StringComparison.OrdinalIgnoreCase) == true
                        ? basicPay : 0m;
                    grossPay += baseValue * (comp.Percentage ?? 0m) / 100m;
                }
            }

            var pfEmployee = _calculator.CalculatePfEmployee(basicPay);
            var pfEmployer = _calculator.CalculatePfEmployer(basicPay);
            var esiEmployee = grossPay <= 21_000m ? (decimal?)_calculator.CalculateEsiEmployee(grossPay) : null;
            var esiEmployer = grossPay <= 21_000m ? (decimal?)_calculator.CalculateEsiEmployer(grossPay) : null;

            // Default to New regime unless employee has PAN (old regime sign-up stored elsewhere)
            var taxRegime = string.IsNullOrEmpty(employee.PanNumber) ? "New" : "New";
            var tds = _calculator.CalculateMonthlyTds(grossPay, pfEmployee, taxRegime);

            var totalDeductions = pfEmployee + (esiEmployee ?? 0m) + tds;
            var netPay = grossPay - totalDeductions;

            var entry = new PayrollEntry
            {
                TenantId = request.TenantId,
                PayrollRunId = run.Id,
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                SalaryStructureId = structure.Id,
                GrossPay = grossPay,
                PfEmployee = pfEmployee,
                PfEmployer = pfEmployer,
                EsiEmployee = esiEmployee,
                EsiEmployer = esiEmployer,
                TdsAmount = tds,
                TotalDeductions = totalDeductions,
                NetPay = netPay,
                TaxRegime = taxRegime
            };

            run.Entries.Add(entry);
        }

        run.TotalGrossPay = run.Entries.Sum(e => e.GrossPay);
        run.TotalDeductions = run.Entries.Sum(e => e.TotalDeductions);
        run.TotalNetPay = run.Entries.Sum(e => e.NetPay);

        _db.PayrollRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(run.Id);
    }
}
