using ERP.Shared.Domain;

namespace ERP.LMS.Domain;

public class QuizQuestion : TenantEntity
{
    public Guid QuizId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType QuestionType { get; set; }
    // JSON array of strings for MCQ/TrueFalse options
    public string? Options { get; set; }
    // MCQ: index string ("0","1"…), TrueFalse: "True"/"False", ShortAnswer: null
    public string? CorrectAnswer { get; set; }
    public decimal Marks { get; set; }
    public int OrderIndex { get; set; }
    public Quiz? Quiz { get; set; }
}
