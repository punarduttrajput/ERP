using ERP.HRMS.Infrastructure;
using ERP.Shared.Application.Abstractions;
using ERP.Shared.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.HRMS.Application.Commands;

public record GeneratePayslipCommand(Guid PayrollEntryId, Guid TenantId) : IRequest<Result<byte[]>>;

public class GeneratePayslipHandler : IRequestHandler<GeneratePayslipCommand, Result<byte[]>>
{
    private readonly IHrmsDbContext _db;
    private readonly IPdfService _pdf;

    public GeneratePayslipHandler(IHrmsDbContext db, IPdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    public async Task<Result<byte[]>> Handle(GeneratePayslipCommand request, CancellationToken cancellationToken)
    {
        var entry = await _db.PayrollEntries
            .Include(e => e.PayrollRun)
            .FirstOrDefaultAsync(e => e.Id == request.PayrollEntryId && e.TenantId == request.TenantId, cancellationToken);

        if (entry is null)
            return Result.Failure<byte[]>("Payroll entry not found.");

        var employee = await _db.Employees
            .FirstOrDefaultAsync(e => e.Id == entry.EmployeeId && e.TenantId == request.TenantId, cancellationToken);

        if (employee is null)
            return Result.Failure<byte[]>("Employee not found.");

        var monthName = new DateTime(entry.PayrollRun!.Year, entry.PayrollRun.Month, 1).ToString("MMMM yyyy");

        var html = $@"<!DOCTYPE html>
<html>
<head>
<style>
  body {{ font-family: Arial, sans-serif; margin: 40px; color: #333; }}
  h1 {{ color: #2c3e50; border-bottom: 2px solid #2c3e50; padding-bottom: 10px; }}
  .header-info {{ display: flex; justify-content: space-between; margin-bottom: 20px; }}
  table {{ width: 100%; border-collapse: collapse; margin-bottom: 20px; }}
  th {{ background-color: #2c3e50; color: white; padding: 8px 12px; text-align: left; }}
  td {{ padding: 8px 12px; border-bottom: 1px solid #ddd; }}
  .total-row td {{ font-weight: bold; background-color: #ecf0f1; }}
  .net-pay {{ font-size: 1.2em; font-weight: bold; color: #27ae60; text-align: right; margin-top: 10px; }}
</style>
</head>
<body>
<h1>Payslip — {monthName}</h1>
<div class='header-info'>
  <div>
    <strong>Employee:</strong> {employee.FirstName} {employee.LastName}<br/>
    <strong>Employee Code:</strong> {employee.EmployeeCode}<br/>
    <strong>Designation:</strong> {employee.Designation}<br/>
    <strong>Tax Regime:</strong> {entry.TaxRegime}
  </div>
  <div>
    <strong>Pay Period:</strong> {monthName}<br/>
    <strong>PAN:</strong> {employee.PanNumber ?? "N/A"}<br/>
    <strong>Email:</strong> {employee.Email}
  </div>
</div>

<table>
  <tr><th>Earnings</th><th>Amount (₹)</th></tr>
  <tr><td>Gross Pay</td><td>{entry.GrossPay:N2}</td></tr>
  <tr class='total-row'><td>Total Earnings</td><td>{entry.GrossPay:N2}</td></tr>
</table>

<table>
  <tr><th>Deductions</th><th>Amount (₹)</th></tr>
  <tr><td>PF (Employee)</td><td>{entry.PfEmployee:N2}</td></tr>
  {(entry.EsiEmployee.HasValue ? $"<tr><td>ESI (Employee)</td><td>{entry.EsiEmployee.Value:N2}</td></tr>" : "")}
  <tr><td>TDS</td><td>{entry.TdsAmount:N2}</td></tr>
  <tr class='total-row'><td>Total Deductions</td><td>{entry.TotalDeductions:N2}</td></tr>
</table>

<div class='net-pay'>Net Pay: ₹{entry.NetPay:N2}</div>
</body>
</html>";

        var pdfBytes = await _pdf.GeneratePdfAsync(html, cancellationToken);

        entry.PayslipGenerated = true;
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success(pdfBytes);
    }
}
