using ERP.Exams.Domain;

namespace ERP.Exams.Application.Services;

public sealed class GpaCalculatorService
{
    /// <summary>
    /// GPA = Σ(GradePoints × Credits) / Σ(Credits) for all subjects in a semester.
    /// </summary>
    public decimal CalculateGpa(IReadOnlyList<(decimal gradePoints, int credits)> subjects)
    {
        if (subjects.Count == 0)
            return 0m;

        var totalWeighted = subjects.Sum(s => s.gradePoints * s.credits);
        var totalCredits = subjects.Sum(s => s.credits);

        if (totalCredits == 0)
            return 0m;

        return Math.Round(totalWeighted / totalCredits, 2);
    }

    /// <summary>
    /// CGPA = Σ(GPA × totalCreditsInSemester) / Σ(allCredits) across all semesters.
    /// </summary>
    public decimal CalculateCgpa(IReadOnlyList<(decimal gpa, int semesterCredits)> semesters)
    {
        if (semesters.Count == 0)
            return 0m;

        var totalWeighted = semesters.Sum(s => s.gpa * s.semesterCredits);
        var totalCredits = semesters.Sum(s => s.semesterCredits);

        if (totalCredits == 0)
            return 0m;

        return Math.Round(totalWeighted / totalCredits, 2);
    }

    /// <summary>
    /// Map raw mark to grade letter + grade points from a grading scheme.
    /// Marks are normalised to 100 before applying rules (rules are expressed as percentage-of-MaxMarks).
    /// </summary>
    public (string gradeLetter, decimal gradePoints) GetGrade(
        decimal marks, decimal maxMarks, IReadOnlyList<GradeRule> rules)
    {
        if (maxMarks <= 0)
            return ("F", 0m);

        var percentage = (marks / maxMarks) * 100m;

        // Sort rules descending by MinMarks so we pick the highest matching bracket first
        var sorted = rules.OrderByDescending(r => r.MinMarks).ToList();

        foreach (var rule in sorted)
        {
            if (percentage >= rule.MinMarks)
                return (rule.GradeLetter, rule.GradePoints);
        }

        // Fallback — return the lowest rule (should be "F")
        var fallback = rules.OrderBy(r => r.MinMarks).FirstOrDefault();
        return fallback is null ? ("F", 0m) : (fallback.GradeLetter, fallback.GradePoints);
    }
}
