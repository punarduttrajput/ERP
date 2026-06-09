using ERP.Exams.Application.Services;
using ERP.Exams.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.Exams;

public class GpaCalculatorTests
{
    private readonly GpaCalculatorService _calculator = new();

    [Fact]
    public void Calculate_GPA_Correctly()
    {
        // 3 subjects: gradePoints [9, 8, 7], credits [4, 3, 3]
        // GPA = (9*4 + 8*3 + 7*3) / (4+3+3) = (36+24+21) / 10 = 81/10 = 8.1
        var subjects = new List<(decimal gradePoints, int credits)>
        {
            (9m, 4),
            (8m, 3),
            (7m, 3)
        };

        var gpa = _calculator.CalculateGpa(subjects);

        gpa.Should().Be(8.1m);
    }

    [Fact]
    public void Calculate_CGPA_Correctly()
    {
        // 2 semesters: GPA [8.5, 7.5], credits [30, 32]
        // CGPA = (8.5*30 + 7.5*32) / (30+32) = (255 + 240) / 62 = 495 / 62 ≈ 7.98
        var semesters = new List<(decimal gpa, int semesterCredits)>
        {
            (8.5m, 30),
            (7.5m, 32)
        };

        var cgpa = _calculator.CalculateCgpa(semesters);

        var expected = Math.Round((8.5m * 30 + 7.5m * 32) / (30 + 32), 2);
        cgpa.Should().Be(expected);
    }

    [Fact]
    public void GetGrade_MapsCorrectly_85Marks_ReturnsO_And_10Points()
    {
        // 85/100 on a 10-point scale -> "O" grade, 10 points
        var rules = BuildStandardGradeRules();

        var (gradeLetter, gradePoints) = _calculator.GetGrade(85m, 100m, rules);

        gradeLetter.Should().Be("O");
        gradePoints.Should().Be(10m);
    }

    [Fact]
    public void GetGrade_FailingMarks_ReturnsF_And_0Points()
    {
        // 30/100 -> "F", 0 points
        var rules = BuildStandardGradeRules();

        var (gradeLetter, gradePoints) = _calculator.GetGrade(30m, 100m, rules);

        gradeLetter.Should().Be("F");
        gradePoints.Should().Be(0m);
    }

    private static List<GradeRule> BuildStandardGradeRules()
    {
        var schemeId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        return new List<GradeRule>
        {
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 90m, MaxMarks = 100m, GradeLetter = "O",  GradePoints = 10m },
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 80m, MaxMarks = 89m,  GradeLetter = "A+", GradePoints = 9m  },
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 70m, MaxMarks = 79m,  GradeLetter = "A",  GradePoints = 8m  },
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 60m, MaxMarks = 69m,  GradeLetter = "B+", GradePoints = 7m  },
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 50m, MaxMarks = 59m,  GradeLetter = "B",  GradePoints = 6m  },
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 40m, MaxMarks = 49m,  GradeLetter = "C",  GradePoints = 5m  },
            new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 0m,  MaxMarks = 39m,  GradeLetter = "F",  GradePoints = 0m  }
        };
    }
}
