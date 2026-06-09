using ERP.Exams.Application.Commands;
using ERP.Exams.Application.Services;
using ERP.Exams.Domain;
using ERP.Exams.Infrastructure;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Exams;

/// <summary>
/// In-memory implementation of IExamsDbContext for testing.
/// </summary>
public class InMemoryExamsDbContext : DbContext, IExamsDbContext
{
    public InMemoryExamsDbContext(DbContextOptions<InMemoryExamsDbContext> options) : base(options) { }

    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();
    public DbSet<SeatAllocation> SeatAllocations => Set<SeatAllocation>();
    public DbSet<GradingScheme> GradingSchemes => Set<GradingScheme>();
    public DbSet<GradeRule> GradeRules => Set<GradeRule>();
    public DbSet<InternalMark> InternalMarks => Set<InternalMark>();
    public DbSet<ExternalMark> ExternalMarks => Set<ExternalMark>();
    public DbSet<StudentResult> StudentResults => Set<StudentResult>();
    public DbSet<ArrearRegistration> ArrearRegistrations => Set<ArrearRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExamSchedule>().HasKey(x => x.Id);
        modelBuilder.Entity<SeatAllocation>().HasKey(x => x.Id);
        modelBuilder.Entity<GradingScheme>().HasKey(x => x.Id);
        modelBuilder.Entity<GradeRule>().HasKey(x => x.Id);
        modelBuilder.Entity<InternalMark>().HasKey(x => x.Id);
        modelBuilder.Entity<ExternalMark>().HasKey(x => x.Id);
        modelBuilder.Entity<StudentResult>().HasKey(x => x.Id);
        modelBuilder.Entity<ArrearRegistration>().HasKey(x => x.Id);

        modelBuilder.Entity<GradingScheme>()
            .HasMany(g => g.GradeRules)
            .WithOne(r => r.GradingScheme)
            .HasForeignKey(r => r.GradingSchemeId);
    }
}

public class PublishResultsHandlerTests
{
    private static InMemoryExamsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<InMemoryExamsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InMemoryExamsDbContext(options);
    }

    private static List<GradeRule> BuildGradeRules(Guid schemeId, Guid tenantId) => new()
    {
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 90m, MaxMarks = 100m, GradeLetter = "O",  GradePoints = 10m },
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 80m, MaxMarks = 89m,  GradeLetter = "A+", GradePoints = 9m  },
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 70m, MaxMarks = 79m,  GradeLetter = "A",  GradePoints = 8m  },
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 60m, MaxMarks = 69m,  GradeLetter = "B+", GradePoints = 7m  },
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 50m, MaxMarks = 59m,  GradeLetter = "B",  GradePoints = 6m  },
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 40m, MaxMarks = 49m,  GradeLetter = "C",  GradePoints = 5m  },
        new() { TenantId = tenantId, GradingSchemeId = schemeId, MinMarks = 0m,  MaxMarks = 39m,  GradeLetter = "F",  GradePoints = 0m  }
    };

    [Fact]
    public async Task PublishResults_SetsIsPublishedTrue()
    {
        // Arrange
        var db = CreateDb();
        var mediator = new Mock<IMediator>();
        var tenantId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        var scheduleId = Guid.NewGuid();
        db.ExamSchedules.Add(new ExamSchedule
        {
            Id = scheduleId,
            TenantId = tenantId,
            SemesterId = semesterId,
            SubjectId = subjectId,
            SubjectName = "Mathematics",
            ExamDate = new DateOnly(2026, 6, 10),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            Venue = "Hall A",
            MaxMarks = 100,
            PassingMarks = 40
        });

        var schemeId = Guid.NewGuid();
        var scheme = new GradingScheme
        {
            Id = schemeId,
            TenantId = tenantId,
            Name = "10-point CGPA",
            IsDefault = true
        };
        db.GradingSchemes.Add(scheme);

        foreach (var rule in BuildGradeRules(schemeId, tenantId))
            db.GradeRules.Add(rule);

        db.InternalMarks.Add(new InternalMark
        {
            TenantId = tenantId,
            SubjectId = subjectId,
            StudentId = studentId,
            SemesterId = semesterId,
            Marks = 40m,
            MaxMarks = 50m,
            EnteredBy = Guid.NewGuid()
        });

        db.ExternalMarks.Add(new ExternalMark
        {
            TenantId = tenantId,
            ExamScheduleId = scheduleId,
            StudentId = studentId,
            Marks = 50m,
            MaxMarks = 100m,
            IsAbsent = false,
            EnteredBy = Guid.NewGuid()
        });

        await db.SaveChangesAsync();

        var handler = new PublishResultsHandler(db, mediator.Object);

        // Act
        var result = await handler.Handle(new PublishResultsCommand(tenantId, semesterId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var publishedResults = db.StudentResults.Where(r => r.SemesterId == semesterId).ToList();
        publishedResults.Should().NotBeEmpty();
        publishedResults.All(r => r.IsPublished).Should().BeTrue();
        publishedResults.All(r => r.PublishedAt.HasValue).Should().BeTrue();
    }

    [Fact]
    public async Task PublishResults_CalculatesGpaCorrectly()
    {
        // Arrange — 2 subjects for same student
        var db = CreateDb();
        var mediator = new Mock<IMediator>();
        var tenantId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var schemeId = Guid.NewGuid();
        var scheme = new GradingScheme
        {
            Id = schemeId,
            TenantId = tenantId,
            Name = "10-point CGPA",
            IsDefault = true
        };
        db.GradingSchemes.Add(scheme);

        foreach (var rule in BuildGradeRules(schemeId, tenantId))
            db.GradeRules.Add(rule);

        // Subject 1: 90/150 total = 60% -> B+ (7 pts), credits=3
        // Subject 2: 120/150 total = 80% -> A+ (9 pts), credits=3
        // GPA = (7*3 + 9*3)/(3+3) = 48/6 = 8.0

        Guid subj1 = Guid.NewGuid(), subj2 = Guid.NewGuid();
        Guid sched1 = Guid.NewGuid(), sched2 = Guid.NewGuid();

        db.ExamSchedules.Add(new ExamSchedule { Id = sched1, TenantId = tenantId, SemesterId = semesterId, SubjectId = subj1, SubjectName = "Maths", ExamDate = new DateOnly(2026, 6, 10), StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(12,0), Venue = "A", MaxMarks = 100, PassingMarks = 40 });
        db.ExamSchedules.Add(new ExamSchedule { Id = sched2, TenantId = tenantId, SemesterId = semesterId, SubjectId = subj2, SubjectName = "Physics", ExamDate = new DateOnly(2026, 6, 11), StartTime = new TimeOnly(9,0), EndTime = new TimeOnly(12,0), Venue = "A", MaxMarks = 100, PassingMarks = 40 });

        // Internal marks: 40/50 for both subjects
        db.InternalMarks.Add(new InternalMark { TenantId = tenantId, SubjectId = subj1, StudentId = studentId, SemesterId = semesterId, Marks = 40m, MaxMarks = 50m, EnteredBy = Guid.NewGuid() });
        db.InternalMarks.Add(new InternalMark { TenantId = tenantId, SubjectId = subj2, StudentId = studentId, SemesterId = semesterId, Marks = 40m, MaxMarks = 50m, EnteredBy = Guid.NewGuid() });

        // External marks: 50/100 for subj1 -> total 90/150 = 60%; 80/100 for subj2 -> total 120/150 = 80%
        db.ExternalMarks.Add(new ExternalMark { TenantId = tenantId, ExamScheduleId = sched1, StudentId = studentId, Marks = 50m, MaxMarks = 100m, IsAbsent = false, EnteredBy = Guid.NewGuid() });
        db.ExternalMarks.Add(new ExternalMark { TenantId = tenantId, ExamScheduleId = sched2, StudentId = studentId, Marks = 80m, MaxMarks = 100m, IsAbsent = false, EnteredBy = Guid.NewGuid() });

        await db.SaveChangesAsync();

        var handler = new PublishResultsHandler(db, mediator.Object);

        // Act
        var result = await handler.Handle(new PublishResultsCommand(tenantId, semesterId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var studentResults = db.StudentResults.Where(r => r.StudentId == studentId && r.SemesterId == semesterId).ToList();
        studentResults.Should().HaveCount(2);

        var calculator = new GpaCalculatorService();
        var gradeRules = BuildGradeRules(schemeId, tenantId);

        var (_, gpSubj1) = calculator.GetGrade(90m, 150m, gradeRules);  // 60% -> B+, 7pts
        var (_, gpSubj2) = calculator.GetGrade(120m, 150m, gradeRules); // 80% -> A+, 9pts
        var expectedGpa = calculator.CalculateGpa(new[] { (gpSubj1, 3), (gpSubj2, 3) }.ToList());

        studentResults.All(r => r.GPA == expectedGpa).Should().BeTrue();
    }

    [Fact]
    public async Task GetHallTicket_IneligibleStudent_ReturnsFailure()
    {
        // Arrange
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var semesterId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();

        db.ExamSchedules.Add(new ExamSchedule
        {
            Id = scheduleId,
            TenantId = tenantId,
            SemesterId = semesterId,
            SubjectId = subjectId,
            SubjectName = "Chemistry",
            ExamDate = new DateOnly(2026, 6, 12),
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            Venue = "Hall B",
            MaxMarks = 100,
            PassingMarks = 40
        });

        // Student has a seat allocation but is ineligible (internal marks pending)
        db.SeatAllocations.Add(new SeatAllocation
        {
            TenantId = tenantId,
            ExamScheduleId = scheduleId,
            StudentId = studentId,
            RollNumber = "CS2021001",
            SeatNumber = "HABC-01-01",
            HallTicketGenerated = false,
            IsEligible = false,
            IneligibilityReason = "Internal marks pending."
        });

        await db.SaveChangesAsync();

        var handler = new ERP.Exams.Application.Queries.GetHallTicketHandler(db);

        // Act
        var result = await handler.Handle(
            new ERP.Exams.Application.Queries.GetHallTicketQuery(studentId, scheduleId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internal marks pending.");
    }
}
