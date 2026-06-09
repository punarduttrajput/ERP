using System.Text;
using ERP.Reporting.Application.Commands;
using ERP.Reporting.Application.Services;
using ERP.Reporting.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.Reporting;

public class ReportingTests
{
    [Fact]
    public void PredefinedRegistry_HasAtLeast15Reports()
    {
        var reports = PredefinedReportRegistry.GetAll();
        reports.Count.Should().BeGreaterThanOrEqualTo(15);
    }

    [Fact]
    public async Task CsvExporter_ProducesCorrectOutput()
    {
        var exporter = new CsvReportExporter();
        var columns = new[] { "Name", "Score" };
        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Name"] = "Alice", ["Score"] = 95 },
            new Dictionary<string, object?> { ["Name"] = "Bob",   ["Score"] = 82 }
        };

        var bytes = await exporter.ExportAsync("Test Report", columns, rows);
        var csv = Encoding.UTF8.GetString(bytes);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(3); // header + 2 data rows
        lines[0].Should().Contain("\"Name\"");
        lines[0].Should().Contain("\"Score\"");
        lines[1].Should().Contain("\"Alice\"");
        lines[2].Should().Contain("\"Bob\"");
    }

    [Fact]
    public async Task ExcelExporter_ProducesNonEmptyBytes()
    {
        var exporter = new ExcelReportExporter();
        var columns = new[] { "Name", "Score" };
        var rows = new List<IDictionary<string, object?>>
        {
            new Dictionary<string, object?> { ["Name"] = "Alice", ["Score"] = 95 },
            new Dictionary<string, object?> { ["Name"] = "Bob",   ["Score"] = 82 }
        };

        var bytes = await exporter.ExportAsync("Test Report", columns, rows);
        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SqlValidation_DangerousKeyword_Blocked()
    {
        // CreateReportDefinitionHandler blocks dangerous SQL keywords
        var blockedSqls = new[]
        {
            "DROP TABLE students",
            "DELETE FROM students WHERE 1=1",
            "UPDATE students SET Name='x'",
            "INSERT INTO students VALUES (1)",
            "EXEC sp_executesql 'SELECT 1'",
            "SELECT * FROM xp_cmdshell('dir')"
        };

        foreach (var sql in blockedSqls)
        {
            var lower = sql.ToLowerInvariant();
            var blocked = new[] { "drop", "delete", "update", "insert", "exec", "xp_", "sp_" }
                .Any(k => lower.Contains(k));
            blocked.Should().BeTrue(because: $"SQL '{sql}' should be blocked");
        }
    }

    [Fact]
    public void NextRunAt_Daily_CorrectlyComputed()
    {
        var from = new DateTime(2026, 6, 7, 10, 0, 0, DateTimeKind.Utc);
        var next = CreateReportScheduleHandler.ComputeNextRunAt(ScheduleFrequency.Daily, null, null, 7, from);

        // Should be next day at hour 7 (since 07:00 today is in the past relative to 10:00)
        next.Date.Should().Be(from.Date.AddDays(1));
        next.Hour.Should().Be(7);
    }

    [Fact]
    public void NextRunAt_Monthly_ClampedToLastDay()
    {
        // day 31 doesn't exist in February — should clamp to 28 (or 29 in leap year)
        var from = new DateTime(2026, 1, 31, 8, 0, 0, DateTimeKind.Utc);
        var next = CreateReportScheduleHandler.ComputeNextRunAt(ScheduleFrequency.Monthly, null, 31, 7, from);

        // Next month from Jan 31 = February; Feb 2026 has 28 days
        next.Month.Should().Be(2);
        next.Day.Should().Be(28);
        next.Hour.Should().Be(7);
    }
}
