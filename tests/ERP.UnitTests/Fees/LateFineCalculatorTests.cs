using ERP.Fees.Application.Services;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.Fees;

public class LateFineCalculatorTests
{
    private readonly LateFineCalculatorService _sut = new();

    [Fact]
    public void NoLateFine_WhenPaidOnTime()
    {
        var dueDate = new DateOnly(2026, 6, 1);
        var paymentDate = new DateOnly(2026, 6, 1);

        var fine = _sut.Calculate(dueDate, paymentDate, finePerDay: 50, maxFine: 1000);

        fine.Should().Be(0);
    }

    [Fact]
    public void CorrectFine_WhenOneDayLate()
    {
        var dueDate = new DateOnly(2026, 6, 1);
        var paymentDate = new DateOnly(2026, 6, 2);

        var fine = _sut.Calculate(dueDate, paymentDate, finePerDay: 50, maxFine: 1000);

        fine.Should().Be(50);
    }

    [Fact]
    public void CapApplied_WhenMaxFineReached()
    {
        var dueDate = new DateOnly(2026, 5, 1);
        var paymentDate = new DateOnly(2026, 5, 31);

        var fine = _sut.Calculate(dueDate, paymentDate, finePerDay: 100, maxFine: 1000);

        fine.Should().Be(1000);
    }

    [Fact]
    public void ZeroFine_WhenPaidBeforeDue()
    {
        var dueDate = new DateOnly(2026, 6, 10);
        var paymentDate = new DateOnly(2026, 6, 5);

        var fine = _sut.Calculate(dueDate, paymentDate, finePerDay: 50, maxFine: 1000);

        fine.Should().Be(0);
    }
}
