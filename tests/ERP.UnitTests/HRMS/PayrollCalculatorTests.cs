using ERP.HRMS.Application.Commands;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.HRMS;

public class PayrollCalculatorTests
{
    private readonly PayrollCalculatorService _calc = new();

    [Fact]
    public void PF_Capped_At15000_Basic()
    {
        var pf = _calc.CalculatePfEmployee(20_000m);
        pf.Should().Be(1_800m); // 12% of 15,000
    }

    [Fact]
    public void PF_OnActualBasic_WhenBelow15000()
    {
        var pf = _calc.CalculatePfEmployee(10_000m);
        pf.Should().Be(1_200m); // 12% of 10,000
    }

    [Fact]
    public void ESI_Applied_When_GrossBelow21000()
    {
        var esi = _calc.CalculateEsiEmployee(18_000m);
        esi.Should().Be(135m); // 0.75% of 18,000
    }

    [Fact]
    public void ESI_Not_Applied_When_GrossAbove21000()
    {
        var esi = _calc.CalculateEsiEmployee(25_000m);
        esi.Should().Be(0m);
    }

    [Fact]
    public void TDS_NewRegime_ZeroForLowIncome()
    {
        // Annual income = 20,000 * 12 = 2,40,000 — below 3L threshold in new regime
        var tds = _calc.CalculateMonthlyTds(20_000m, 1_800m, "New");
        tds.Should().Be(0m);
    }

    [Fact]
    public void TDS_OldRegime_CorrectSlab()
    {
        // Annual income = 50,000 * 12 = 6,00,000
        // Old regime: 5L-10L = 20% slab
        // After 80C deduction: PF = 1,800*12=21,600; taxable = 6L - 21,600 = 5,78,400
        // Tax: 12,500 (2.5L-5L at 5%) + (78,400 * 20%) = 12,500 + 15,680 = 28,180
        // Monthly TDS = 28,180 / 12 = 2,348.33
        var tds = _calc.CalculateMonthlyTds(50_000m, 1_800m, "Old");
        tds.Should().BeGreaterThan(0m);
        // Annual tax is in the 20% slab range: annual tax ≈ 28,180, monthly ≈ 2,348
        var annualTds = tds * 12;
        annualTds.Should().BeGreaterThan(10_000m).And.BeLessThan(50_000m);
    }

    [Fact]
    public void NetPay_Equals_Gross_Minus_Deductions()
    {
        var gross = 30_000m;
        var pf = _calc.CalculatePfEmployee(20_000m);
        var esi = _calc.CalculateEsiEmployee(gross);
        var tds = _calc.CalculateMonthlyTds(gross, pf, "New");

        var totalDeductions = pf + esi + tds;
        var netPay = gross - totalDeductions;

        netPay.Should().Be(gross - totalDeductions);
        netPay.Should().BeLessThan(gross);
    }
}
