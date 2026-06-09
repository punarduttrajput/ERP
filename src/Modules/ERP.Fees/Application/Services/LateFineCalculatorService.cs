namespace ERP.Fees.Application.Services;

public class LateFineCalculatorService
{
    public decimal Calculate(DateOnly dueDate, DateOnly paymentDate, decimal finePerDay, decimal maxFine)
    {
        var daysLate = Math.Max(0, (paymentDate.DayNumber - dueDate.DayNumber));
        return Math.Min(daysLate * finePerDay, maxFine);
    }
}
