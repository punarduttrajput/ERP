using ERP.Fees.Application.Events;
using ERP.Finance.Application.Commands;
using ERP.Finance.Application.Events;
using ERP.Finance.Application.Handlers;
using ERP.Finance.Application.Queries;
using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace ERP.UnitTests.Finance;

public class FeePaymentGlHandlerTests
{
    private static (TestFinanceDbContext ctx, IMediator mediator) BuildStack(Guid tenantId, string receivableCode = "1100", string cashCode = "1010")
    {
        var options = new DbContextOptionsBuilder<TestFinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new TestFinanceDbContext(options, tenantId);

        var receivable = new Account { Id = Guid.NewGuid(), TenantId = tenantId, Code = receivableCode, Name = "AR Students", AccountType = AccountType.Asset, IsActive = true, IsControl = false };
        var cash = new Account { Id = Guid.NewGuid(), TenantId = tenantId, Code = cashCode, Name = "Main Bank", AccountType = AccountType.Asset, IsActive = true, IsControl = false };
        ctx.GlAccounts.AddRange(receivable, cash);
        ctx.SaveChanges();

        var mediatorMock = new Mock<IMediator>();

        var createHandler = new CreateJournalEntryHandler(ctx);
        var postHandler = new PostJournalEntryHandler(ctx);
        var findHandler = new FindAccountByCodeHandler(ctx);

        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateJournalEntryCommand>(), It.IsAny<CancellationToken>()))
            .Returns<CreateJournalEntryCommand, CancellationToken>((cmd, ct) => createHandler.Handle(cmd, ct));

        mediatorMock
            .Setup(m => m.Send(It.IsAny<PostJournalEntryCommand>(), It.IsAny<CancellationToken>()))
            .Returns<PostJournalEntryCommand, CancellationToken>((cmd, ct) => postHandler.Handle(cmd, ct));

        mediatorMock
            .Setup(m => m.Send(It.IsAny<FindAccountByCodeQuery>(), It.IsAny<CancellationToken>()))
            .Returns<FindAccountByCodeQuery, CancellationToken>((cmd, ct) => findHandler.Handle(cmd, ct));

        mediatorMock
            .Setup(m => m.Publish(It.IsAny<FeePaymentGlPostedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return (ctx, mediatorMock.Object);
    }

    private static IConfiguration BuildConfig(string receivableCode = "1100", string cashCode = "1010")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Finance:FeeReceivableAccountCode"] = receivableCode,
                ["Finance:CashAccountCode"] = cashCode
            })
            .Build();
    }

    [Fact]
    public async Task FeePaymentReceived_CreatesAndPostsJournalEntry()
    {
        var tenantId = Guid.NewGuid();
        var (ctx, mediator) = BuildStack(tenantId);
        var config = BuildConfig();
        var handler = new FeePaymentReceivedEventHandler(mediator, config);

        var evt = new FeePaymentReceivedEvent(tenantId, Guid.NewGuid(), Guid.NewGuid(), 5000m, "RCT-001", DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        var entries = ctx.JournalEntries.Include(e => e.Lines).ToList();
        entries.Should().HaveCount(1);
        entries[0].Status.Should().Be(EntryStatus.Posted);
    }

    [Fact]
    public async Task FeePaymentReceived_DebitMatchesAmount()
    {
        var tenantId = Guid.NewGuid();
        var (ctx, mediator) = BuildStack(tenantId);
        var handler = new FeePaymentReceivedEventHandler(mediator, BuildConfig());

        var amount = 3500m;
        var evt = new FeePaymentReceivedEvent(tenantId, Guid.NewGuid(), Guid.NewGuid(), amount, "RCT-002", DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        var entry = ctx.JournalEntries.Include(e => e.Lines).Single();
        var debitLine = entry.Lines.First(l => l.Debit > 0);
        debitLine.Debit.Should().Be(amount);
    }

    [Fact]
    public async Task FeePaymentReceived_CreditMatchesAmount()
    {
        var tenantId = Guid.NewGuid();
        var (ctx, mediator) = BuildStack(tenantId);
        var handler = new FeePaymentReceivedEventHandler(mediator, BuildConfig());

        var amount = 4200m;
        var evt = new FeePaymentReceivedEvent(tenantId, Guid.NewGuid(), Guid.NewGuid(), amount, "RCT-003", DateTime.UtcNow);

        await handler.Handle(evt, CancellationToken.None);

        var entry = ctx.JournalEntries.Include(e => e.Lines).Single();
        var creditLine = entry.Lines.First(l => l.Credit > 0);
        creditLine.Credit.Should().Be(amount);
    }
}
