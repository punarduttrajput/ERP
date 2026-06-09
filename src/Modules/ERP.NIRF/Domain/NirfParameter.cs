namespace ERP.NIRF.Domain;

public static class NirfParameter
{
    public const string TeachingLearning = "TeachingLearning";
    public const string Research = "Research";
    public const string GraduationOutcomes = "GraduationOutcomes";
    public const string Outreach = "Outreach";
    public const string Perception = "Perception";

    public static readonly IReadOnlyList<string> All = new[]
    {
        TeachingLearning,
        Research,
        GraduationOutcomes,
        Outreach,
        Perception
    };
}
