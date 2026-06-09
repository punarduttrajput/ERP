using ERP.LMS.Application.Commands;
using ERP.LMS.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.LMS;

public class QuizGradingTests
{
    private static QuizQuestion MakeQuestion(QuestionType type, string? correctAnswer, decimal marks) => new()
    {
        Id            = Guid.NewGuid(),
        QuizId        = Guid.NewGuid(),
        TenantId      = Guid.NewGuid(),
        QuestionText  = "Test question",
        QuestionType  = type,
        CorrectAnswer = correctAnswer,
        Marks         = marks,
        OrderIndex    = 1
    };

    private static (bool? isCorrect, decimal? marksAwarded) Grade(QuizQuestion question, string? answer)
    {
        bool? isCorrect = null;
        decimal? marksAwarded = null;

        if (question.QuestionType == QuestionType.MultipleChoice || question.QuestionType == QuestionType.TrueFalse)
        {
            isCorrect    = answer == question.CorrectAnswer;
            marksAwarded = isCorrect == true ? question.Marks : 0m;
        }

        return (isCorrect, marksAwarded);
    }

    [Fact]
    public void MCQ_CorrectAnswer_AwardsFullMarks()
    {
        var question = MakeQuestion(QuestionType.MultipleChoice, "0", 5m);
        var (isCorrect, marksAwarded) = Grade(question, "0");

        isCorrect.Should().BeTrue();
        marksAwarded.Should().Be(5m);
    }

    [Fact]
    public void MCQ_WrongAnswer_AwardsZero()
    {
        var question = MakeQuestion(QuestionType.MultipleChoice, "0", 5m);
        var (isCorrect, marksAwarded) = Grade(question, "1");

        isCorrect.Should().BeFalse();
        marksAwarded.Should().Be(0m);
    }

    [Fact]
    public void TrueFalse_Correct()
    {
        var question = MakeQuestion(QuestionType.TrueFalse, "True", 2m);
        var (isCorrect, marksAwarded) = Grade(question, "True");

        isCorrect.Should().BeTrue();
        marksAwarded.Should().Be(2m);
    }

    [Fact]
    public void ShortAnswer_LeavesIsCorrectNull()
    {
        var question = MakeQuestion(QuestionType.ShortAnswer, null, 10m);
        var (isCorrect, marksAwarded) = Grade(question, "some answer");

        isCorrect.Should().BeNull();
        marksAwarded.Should().BeNull();
    }

    [Fact]
    public void TotalMarks_SumsOnlyNonNullAwards()
    {
        var mcq1 = MakeQuestion(QuestionType.MultipleChoice, "0", 5m);
        var mcq2 = MakeQuestion(QuestionType.MultipleChoice, "1", 5m);
        var sa   = MakeQuestion(QuestionType.ShortAnswer, null, 10m);

        var answers = new[]
        {
            (Question: mcq1, AnswerText: "0"),
            (Question: mcq2, AnswerText: "2"), // wrong
            (Question: sa,   AnswerText: "essay text")
        };

        var total = answers
            .Where(x => x.Question.QuestionType != QuestionType.ShortAnswer)
            .Sum(x => string.Equals(x.AnswerText, x.Question.CorrectAnswer, StringComparison.Ordinal)
                ? x.Question.Marks : 0m);

        total.Should().Be(5m); // only mcq1 correct; mcq2 wrong; sa excluded
    }
}
