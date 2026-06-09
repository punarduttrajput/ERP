namespace ERP.HRMS.Application.Commands;

public class PayrollCalculatorService
{
    private const decimal PfCap = 15_000m;
    private const decimal PfRate = 0.12m;
    private const decimal EsiGrossCap = 21_000m;
    private const decimal EsiEmployeeRate = 0.0075m;
    private const decimal EsiEmployerRate = 0.0325m;

    public decimal CalculatePfEmployee(decimal basicPay)
    {
        var pfBasic = Math.Min(basicPay, PfCap);
        return Math.Round(pfBasic * PfRate, 2);
    }

    public decimal CalculatePfEmployer(decimal basicPay)
    {
        var pfBasic = Math.Min(basicPay, PfCap);
        return Math.Round(pfBasic * PfRate, 2);
    }

    public decimal CalculateEsiEmployee(decimal grossPay)
    {
        if (grossPay > EsiGrossCap)
            return 0m;
        return Math.Round(grossPay * EsiEmployeeRate, 2);
    }

    public decimal CalculateEsiEmployer(decimal grossPay)
    {
        if (grossPay > EsiGrossCap)
            return 0m;
        return Math.Round(grossPay * EsiEmployerRate, 2);
    }

    public decimal CalculateMonthlyTds(decimal grossPay, decimal pfEmployee, string taxRegime)
    {
        var annualIncome = grossPay * 12;
        // PF employee contribution deductible under 80C (old regime only), capped at 1.5L
        var annualTax = taxRegime.Equals("Old", StringComparison.OrdinalIgnoreCase)
            ? CalculateOldRegimeTax(annualIncome, pfEmployee * 12)
            : CalculateNewRegimeTax(annualIncome);

        var monthlyTds = annualTax / 12;
        // Clamp to zero — no negative TDS
        return Math.Max(0m, Math.Round(monthlyTds, 2));
    }

    private static decimal CalculateOldRegimeTax(decimal annualIncome, decimal annualPf)
    {
        var deduction80C = Math.Min(annualPf, 150_000m);
        var taxableIncome = annualIncome - deduction80C;
        if (taxableIncome <= 0) return 0m;

        decimal tax = 0m;
        if (taxableIncome <= 250_000m) return 0m;
        if (taxableIncome <= 500_000m)
            tax = (taxableIncome - 250_000m) * 0.05m;
        else if (taxableIncome <= 1_000_000m)
            tax = 12_500m + (taxableIncome - 500_000m) * 0.20m;
        else
            tax = 112_500m + (taxableIncome - 1_000_000m) * 0.30m;

        return tax;
    }

    private static decimal CalculateNewRegimeTax(decimal annualIncome)
    {
        // New regime 2024: no standard deductions
        if (annualIncome <= 300_000m) return 0m;

        decimal tax = 0m;
        if (annualIncome <= 700_000m)
            tax = (annualIncome - 300_000m) * 0.05m;
        else if (annualIncome <= 1_000_000m)
            tax = 20_000m + (annualIncome - 700_000m) * 0.10m;
        else if (annualIncome <= 1_200_000m)
            tax = 50_000m + (annualIncome - 1_000_000m) * 0.15m;
        else if (annualIncome <= 1_500_000m)
            tax = 80_000m + (annualIncome - 1_200_000m) * 0.20m;
        else
            tax = 140_000m + (annualIncome - 1_500_000m) * 0.30m;

        return tax;
    }
}
