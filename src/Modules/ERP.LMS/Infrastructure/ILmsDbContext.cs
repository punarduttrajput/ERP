using ERP.LMS.Domain;
using Microsoft.EntityFrameworkCore;

namespace ERP.LMS.Infrastructure;

public interface ILmsDbContext
{
    DbSet<CourseContent> CourseContents { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<AssignmentSubmission> AssignmentSubmissions { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<QuizQuestion> QuizQuestions { get; }
    DbSet<QuizAttempt> QuizAttempts { get; }
    DbSet<QuizAnswer> QuizAnswers { get; }
    DbSet<ForumThread> ForumThreads { get; }
    DbSet<ForumReply> ForumReplies { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<StudentProgress> StudentProgresses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
