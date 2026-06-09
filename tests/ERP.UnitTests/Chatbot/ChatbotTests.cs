using ERP.Chatbot.Application.Commands;
using ERP.Chatbot.Application.Services;
using ERP.Chatbot.Application.Services.IntentHandlers;
using ERP.Chatbot.Domain;
using ERP.Chatbot.Infrastructure;
using ERP.Fees.Domain;
using ERP.Fees.Infrastructure;
using ERP.Shared.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Chatbot;

public class ChatbotTestDbContext : DbContext, IChatbotDbContext
{
    public ChatbotTestDbContext(DbContextOptions<ChatbotTestDbContext> options) : base(options) { }

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
}

public class FeesTestDbContext : DbContext, IFeesDbContext
{
    public FeesTestDbContext(DbContextOptions<FeesTestDbContext> options) : base(options) { }

    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeComponent> FeeComponents => Set<FeeComponent>();
    public DbSet<InstallmentSchedule> InstallmentSchedules => Set<InstallmentSchedule>();
    public DbSet<StudentFeeAccount> StudentFeeAccounts => Set<StudentFeeAccount>();
    public DbSet<FeeInstallment> FeeInstallments => Set<FeeInstallment>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<StudentScholarship> StudentScholarships => Set<StudentScholarship>();
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
}

public class ChatbotTests
{
    private static ChatbotTestDbContext CreateChatbotDb()
    {
        var options = new DbContextOptionsBuilder<ChatbotTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ChatbotTestDbContext(options);
    }

    private static FeesTestDbContext CreateFeesDb()
    {
        var options = new DbContextOptionsBuilder<FeesTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FeesTestDbContext(options);
    }

    private static RuleBasedIntentRouter CreateRouter(IFeesDbContext? feesDb = null)
    {
        var feesDbContext = feesDb ?? CreateFeesDb();
        return new RuleBasedIntentRouter(
            new FeeBalanceIntentHandler(feesDbContext),
            new ExamScheduleIntentHandler(new Mock<ERP.Exams.Infrastructure.IExamsDbContext>().Object),
            new TimetableIntentHandler(new Mock<ERP.Timetable.Infrastructure.ITimetableDbContext>().Object),
            new AttendanceSummaryIntentHandler(new Mock<ERP.Attendance.Infrastructure.IAttendanceDbContext>().Object),
            new FacultyBriefIntentHandler(
                new Mock<ERP.Attendance.Infrastructure.IAttendanceDbContext>().Object,
                new Mock<ERP.LMS.Infrastructure.ILmsDbContext>().Object),
            new KpiQueryIntentHandler(
                new Mock<ERP.Accreditation.Infrastructure.IAccreditationDbContext>().Object,
                feesDbContext));
    }

    [Fact]
    public async Task IntentDetection_FeeKeyword_ReturnsFeeBalance()
    {
        var router = CreateRouter();
        var intent = await router.DetectIntentAsync("what is my fee balance?");
        Assert.Equal(ChatIntent.FeeBalance, intent);
    }

    [Fact]
    public async Task IntentDetection_ExamKeyword_ReturnsExamSchedule()
    {
        var router = CreateRouter();
        var intent = await router.DetectIntentAsync("show me my exam schedule");
        Assert.Equal(ChatIntent.ExamSchedule, intent);
    }

    [Fact]
    public async Task IntentDetection_GreetingKeyword_ReturnsGreeting()
    {
        var router = CreateRouter();
        var intent = await router.DetectIntentAsync("hello");
        Assert.Equal(ChatIntent.Greeting, intent);
    }

    [Fact]
    public async Task IntentDetection_UnknownMessage_ReturnsUnknown()
    {
        var router = CreateRouter();
        var intent = await router.DetectIntentAsync("pizza");
        Assert.Equal(ChatIntent.Unknown, intent);
    }

    [Fact]
    public async Task SendMessage_FeeIntent_ReturnsNoDuesMessage()
    {
        var feesDb = CreateFeesDb();
        var chatDb = CreateChatbotDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.GetAsync<List<CachedMessage>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((List<CachedMessage>?)null);
        cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<List<CachedMessage>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var router = CreateRouter(feesDb);

        var handler = new SendMessageHandler(
            chatDb,
            router,
            cacheMock.Object,
            new FeeBalanceIntentHandler(feesDb),
            new ExamScheduleIntentHandler(new Mock<ERP.Exams.Infrastructure.IExamsDbContext>().Object),
            new TimetableIntentHandler(new Mock<ERP.Timetable.Infrastructure.ITimetableDbContext>().Object),
            new AttendanceSummaryIntentHandler(new Mock<ERP.Attendance.Infrastructure.IAttendanceDbContext>().Object),
            new FacultyBriefIntentHandler(
                new Mock<ERP.Attendance.Infrastructure.IAttendanceDbContext>().Object,
                new Mock<ERP.LMS.Infrastructure.ILmsDbContext>().Object),
            new KpiQueryIntentHandler(
                new Mock<ERP.Accreditation.Infrastructure.IAccreditationDbContext>().Object,
                feesDb));

        var result = await handler.Handle(
            new SendMessageCommand(tenantId, userId, "what is my fee balance?"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("fully paid", result.Value!.Response);
        Assert.Equal(ChatIntent.FeeBalance, result.Value.Intent);
    }

    [Fact]
    public async Task DeleteConversation_SetsIsDeleted()
    {
        var chatDb = CreateChatbotDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            SessionKey = $"chat:{tenantId}:{userId}:20260604",
            StartedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
        chatDb.ChatSessions.Add(session);
        await chatDb.SaveChangesAsync();

        var cacheMock = new Mock<ICacheService>();
        cacheMock.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var handler = new DeleteConversationHandler(chatDb, cacheMock.Object);
        var result = await handler.Handle(new DeleteConversationCommand(tenantId, userId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await chatDb.ChatSessions.FindAsync(session.Id);
        Assert.True(updated!.IsDeleted);
        Assert.NotNull(updated.DeletedAt);
    }
}
