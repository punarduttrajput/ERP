using ERP.LMS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ERP.LMS.Infrastructure;

public class CourseContentConfiguration : IEntityTypeConfiguration<CourseContent>
{
    public void Configure(EntityTypeBuilder<CourseContent> b)
    {
        b.ToTable("course_contents");
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).HasMaxLength(300).IsRequired();
        b.Property(e => e.Description).HasMaxLength(1000);
        b.Property(e => e.ContentType).HasConversion<int>();
        b.Property(e => e.BlobUrl).HasMaxLength(1000);
        b.Property(e => e.ExternalUrl).HasMaxLength(1000);
        b.Property(e => e.IsVisible).HasDefaultValue(true);
        b.HasIndex(e => new { e.TenantId, e.SubjectId, e.BatchId });
    }
}

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("assignments");
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).HasMaxLength(300).IsRequired();
        b.Property(e => e.Description).HasMaxLength(2000).IsRequired();
        b.Property(e => e.MaxMarks).HasColumnType("decimal(8,2)");
        b.Property(e => e.IsVisible).HasDefaultValue(true);
        b.Property(e => e.AssignmentCreatedBy).HasColumnName("AssignmentCreatedBy");
        b.HasMany(e => e.Submissions).WithOne(s => s.Assignment).HasForeignKey(s => s.AssignmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.TenantId, e.SubjectId, e.BatchId });
    }
}

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> b)
    {
        b.ToTable("assignment_submissions");
        b.HasKey(e => e.Id);
        b.Property(e => e.BlobUrl).HasMaxLength(1000).IsRequired();
        b.Property(e => e.FileName).HasMaxLength(255).IsRequired();
        b.Property(e => e.Status).HasConversion<int>();
        b.Property(e => e.MarksAwarded).HasColumnType("decimal(8,2)");
        b.Property(e => e.FacultyFeedback).HasMaxLength(1000);
        b.HasIndex(e => new { e.TenantId, e.AssignmentId, e.StudentId }).IsUnique();
    }
}

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> b)
    {
        b.ToTable("quizzes");
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).HasMaxLength(300).IsRequired();
        b.Property(e => e.Instructions).HasMaxLength(2000);
        b.Property(e => e.MaxAttempts).HasDefaultValue(1);
        b.Property(e => e.IsVisible).HasDefaultValue(true);
        b.Property(e => e.QuizCreatedBy).HasColumnName("QuizCreatedBy");
        b.HasMany(e => e.Questions).WithOne(q => q.Quiz).HasForeignKey(q => q.QuizId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.TenantId, e.SubjectId, e.BatchId });
    }
}

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> b)
    {
        b.ToTable("quiz_questions");
        b.HasKey(e => e.Id);
        b.Property(e => e.QuestionText).HasMaxLength(2000).IsRequired();
        b.Property(e => e.QuestionType).HasConversion<int>();
        b.Property(e => e.Options).HasMaxLength(2000);
        b.Property(e => e.CorrectAnswer).HasMaxLength(500);
        b.Property(e => e.Marks).HasColumnType("decimal(8,2)");
        b.HasIndex(e => new { e.TenantId, e.QuizId });
    }
}

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> b)
    {
        b.ToTable("quiz_attempts");
        b.HasKey(e => e.Id);
        b.Property(e => e.TotalMarks).HasColumnType("decimal(8,2)");
        b.Property(e => e.IsCompleted).HasDefaultValue(false);
        b.HasMany(e => e.Answers).WithOne(a => a.Attempt).HasForeignKey(a => a.AttemptId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.TenantId, e.QuizId, e.StudentId });
    }
}

public class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> b)
    {
        b.ToTable("quiz_answers");
        b.HasKey(e => e.Id);
        b.Property(e => e.AnswerText).HasMaxLength(500);
        b.Property(e => e.MarksAwarded).HasColumnType("decimal(8,2)");
        b.HasIndex(e => new { e.TenantId, e.AttemptId, e.QuestionId });
    }
}

public class ForumThreadConfiguration : IEntityTypeConfiguration<ForumThread>
{
    public void Configure(EntityTypeBuilder<ForumThread> b)
    {
        b.ToTable("forum_threads");
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).HasMaxLength(300).IsRequired();
        b.Property(e => e.Body).HasMaxLength(5000).IsRequired();
        b.Property(e => e.IsPinned).HasDefaultValue(false);
        b.Property(e => e.ReplyCount).HasDefaultValue(0);
        b.HasMany(e => e.Replies).WithOne(r => r.Thread).HasForeignKey(r => r.ThreadId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.TenantId, e.SubjectId, e.BatchId });
    }
}

public class ForumReplyConfiguration : IEntityTypeConfiguration<ForumReply>
{
    public void Configure(EntityTypeBuilder<ForumReply> b)
    {
        b.ToTable("forum_replies");
        b.HasKey(e => e.Id);
        b.Property(e => e.Body).HasMaxLength(5000).IsRequired();
        b.HasIndex(e => new { e.TenantId, e.ThreadId });
    }
}

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> b)
    {
        b.ToTable("announcements");
        b.HasKey(e => e.Id);
        b.Property(e => e.Title).HasMaxLength(300).IsRequired();
        b.Property(e => e.Body).HasMaxLength(5000).IsRequired();
        b.Property(e => e.IsVisible).HasDefaultValue(true);
        b.HasIndex(e => new { e.TenantId, e.SubjectId, e.BatchId });
    }
}

public class StudentProgressConfiguration : IEntityTypeConfiguration<StudentProgress>
{
    public void Configure(EntityTypeBuilder<StudentProgress> b)
    {
        b.ToTable("student_progress");
        b.HasKey(e => e.Id);
        b.Property(e => e.ContentViewedCount).HasDefaultValue(0);
        b.Property(e => e.TotalContentCount).HasDefaultValue(0);
        b.Property(e => e.AssignmentsSubmitted).HasDefaultValue(0);
        b.Property(e => e.TotalAssignments).HasDefaultValue(0);
        b.Property(e => e.QuizzesTaken).HasDefaultValue(0);
        b.Property(e => e.TotalQuizzes).HasDefaultValue(0);
        b.Property(e => e.AverageQuizScore).HasColumnType("decimal(8,2)").HasDefaultValue(0m);
        b.HasIndex(e => new { e.TenantId, e.StudentId, e.SubjectId, e.BatchId }).IsUnique();
    }
}
