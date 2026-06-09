namespace ERP.NAAC.Domain;

public static class NaacCriteria
{
    public static readonly IReadOnlyList<CriterionDefinition> All = new[]
    {
        new CriterionDefinition("1", "Curricular Aspects",
            new[] { "1.1", "1.2", "1.3", "1.4" }),
        new CriterionDefinition("2", "Teaching-Learning and Evaluation",
            new[] { "2.1", "2.2", "2.3", "2.4", "2.5", "2.6", "2.7" }),
        new CriterionDefinition("3", "Research, Innovations and Extension",
            new[] { "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7" }),
        new CriterionDefinition("4", "Infrastructure and Learning Resources",
            new[] { "4.1", "4.2", "4.3", "4.4" }),
        new CriterionDefinition("5", "Student Support and Progression",
            new[] { "5.1", "5.2", "5.3", "5.4" }),
        new CriterionDefinition("6", "Governance, Leadership and Management",
            new[] { "6.1", "6.2", "6.3", "6.4", "6.5" }),
        new CriterionDefinition("7", "Institutional Values and Best Practices",
            new[] { "7.1", "7.2", "7.3" })
    };
}

public record CriterionDefinition(string Number, string Title, string[] Indicators);
