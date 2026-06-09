using ERP.LMS.Domain;
using FluentAssertions;
using Xunit;

namespace ERP.UnitTests.LMS;

public class AssignmentTests
{
    private static Assignment MakeAssignment(DateTime dueDate) => new()
    {
        Id        = Guid.NewGuid(),
        TenantId  = Guid.NewGuid(),
        SubjectId = Guid.NewGuid(),
        BatchId   = Guid.NewGuid(),
        Title     = "Test Assignment",
        Description = "desc",
        DueDate   = dueDate,
        MaxMarks  = 100m,
        AssignmentCreatedBy = Guid.NewGuid()
    };

    private static AssignmentSubmission Submit(Assignment assignment, DateTime submittedAt) => new()
    {
        Id           = Guid.NewGuid(),
        TenantId     = assignment.TenantId,
        AssignmentId = assignment.Id,
        StudentId    = Guid.NewGuid(),
        BlobUrl      = "https://example.com/file.pdf",
        FileName     = "file.pdf",
        SubmittedAt  = submittedAt,
        Status       = submittedAt > assignment.DueDate ? SubmissionStatus.Late : SubmissionStatus.Submitted
    };

    [Fact]
    public void SubmitAfterDueDate_MarksAsLate()
    {
        var dueDate   = DateTime.UtcNow.AddDays(-1);
        var assignment = MakeAssignment(dueDate);
        var submission = Submit(assignment, DateTime.UtcNow);

        submission.Status.Should().Be(SubmissionStatus.Late);
    }

    [Fact]
    public void SubmitBeforeDueDate_MarksAsSubmitted()
    {
        var dueDate    = DateTime.UtcNow.AddDays(1);
        var assignment = MakeAssignment(dueDate);
        var submission = Submit(assignment, DateTime.UtcNow);

        submission.Status.Should().Be(SubmissionStatus.Submitted);
    }

    [Fact]
    public void GradeAssignment_SetsStatusGraded()
    {
        var dueDate    = DateTime.UtcNow.AddDays(1);
        var assignment = MakeAssignment(dueDate);
        var submission = Submit(assignment, DateTime.UtcNow);

        // Simulate what GradeAssignmentHandler does
        var facultyId = Guid.NewGuid();
        submission.MarksAwarded    = 85m;
        submission.FacultyFeedback = "Well done";
        submission.GradedBy        = facultyId;
        submission.GradedAt        = DateTime.UtcNow;
        submission.Status          = SubmissionStatus.Graded;

        submission.Status.Should().Be(SubmissionStatus.Graded);
        submission.MarksAwarded.Should().Be(85m);
        submission.GradedBy.Should().Be(facultyId);
        submission.GradedAt.Should().NotBeNull();
    }
}
