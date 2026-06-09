using ERP.Finance.Application.Commands;
using ERP.Finance.Domain;
using ERP.Finance.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ERP.UnitTests.Finance;

public class JournalEntryTests
{
    private static IFinanceDbContext BuildContext(Guid tenantId, IEnumerable<Account>? accounts = null)
    {
        var options = new DbContextOptionsBuilder<TestFinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var ctx = new TestFinanceDbContext(options, tenantId);

        if (accounts is not null)
            ctx.GlAccounts.AddRange(accounts);

        ctx.SaveChanges();
        return ctx;
    }

    private static Account MakeAccount(Guid tenantId, string code, AccountType type = AccountType.Asset)
        => new() { Id = Guid.NewGuid(), TenantId = tenantId, Code = code, Name = code, AccountType = type, IsActive = true, IsControl = false };

    [Fact]
    public async Task UnbalancedEntry_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var a1 = MakeAccount(tenantId, "1010");
        var a2 = MakeAccount(tenantId, "4000", AccountType.Income);
        var ctx = BuildContext(tenantId, new[] { a1, a2 });

        var handler = new CreateJournalEntryHandler(ctx);
        var result = await handler.Handle(new CreateJournalEntryCommand(
            tenantId,
            DateOnly.FromDateTime(DateTime.Today),
            "Test",
            null,
            new List<JournalLineInput>
            {
                new(a1.Id, 1000m, 0m, null),
                new(a2.Id, 0m, 500m, null)  // deliberate imbalance
            }
        ), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not balanced");
    }

    [Fact]
    public async Task BalancedEntry_CreatesWithDraftStatus()
    {
        var tenantId = Guid.NewGuid();
        var a1 = MakeAccount(tenantId, "1010");
        var a2 = MakeAccount(tenantId, "4000", AccountType.Income);
        var ctx = BuildContext(tenantId, new[] { a1, a2 });

        var handler = new CreateJournalEntryHandler(ctx);
        var result = await handler.Handle(new CreateJournalEntryCommand(
            tenantId,
            DateOnly.FromDateTime(DateTime.Today),
            "Balanced Entry",
            null,
            new List<JournalLineInput>
            {
                new(a1.Id, 500m, 0m, null),
                new(a2.Id, 0m, 500m, null)
            }
        ), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var entry = ((TestFinanceDbContext)ctx).JournalEntries
            .Include(e => e.Lines)
            .First(e => e.Id == result.Value);

        entry.Status.Should().Be(EntryStatus.Draft);
        entry.TotalDebit.Should().Be(500m);
        entry.TotalCredit.Should().Be(500m);
        entry.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task PostEntry_UpdatesAccountBalances()
    {
        var tenantId = Guid.NewGuid();
        var asset = MakeAccount(tenantId, "1010", AccountType.Asset);
        var income = MakeAccount(tenantId, "4000", AccountType.Income);
        var ctx = BuildContext(tenantId, new[] { asset, income });

        var createHandler = new CreateJournalEntryHandler(ctx);
        var createResult = await createHandler.Handle(new CreateJournalEntryCommand(
            tenantId,
            DateOnly.FromDateTime(DateTime.Today),
            "Post Test",
            null,
            new List<JournalLineInput>
            {
                new(asset.Id, 1000m, 0m, null),
                new(income.Id, 0m, 1000m, null)
            }
        ), CancellationToken.None);

        var postHandler = new PostJournalEntryHandler(ctx);
        var postResult = await postHandler.Handle(
            new PostJournalEntryCommand(tenantId, createResult.Value, null),
            CancellationToken.None);

        postResult.IsSuccess.Should().BeTrue();

        var db = (TestFinanceDbContext)ctx;
        var assetAccount = db.GlAccounts.Find(asset.Id)!;
        var incomeAccount = db.GlAccounts.Find(income.Id)!;

        // Asset: Credit(0) - Debit(1000) = -1000
        assetAccount.Balance.Should().Be(-1000m);
        // Income: Credit(1000) - Debit(0) = 1000
        incomeAccount.Balance.Should().Be(1000m);
    }

    [Fact]
    public async Task ReverseEntry_CreatesOffsettingEntry()
    {
        var tenantId = Guid.NewGuid();
        var asset = MakeAccount(tenantId, "1010", AccountType.Asset);
        var income = MakeAccount(tenantId, "4000", AccountType.Income);
        var ctx = BuildContext(tenantId, new[] { asset, income });

        var mediatorMock = new Mock<MediatR.IMediator>();

        // Wire up mediator to call real handlers
        var createHandler = new CreateJournalEntryHandler(ctx);
        var postHandler = new PostJournalEntryHandler(ctx);

        mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateJournalEntryCommand>(), It.IsAny<CancellationToken>()))
            .Returns<CreateJournalEntryCommand, CancellationToken>((cmd, ct) => createHandler.Handle(cmd, ct));

        mediatorMock
            .Setup(m => m.Send(It.IsAny<PostJournalEntryCommand>(), It.IsAny<CancellationToken>()))
            .Returns<PostJournalEntryCommand, CancellationToken>((cmd, ct) => postHandler.Handle(cmd, ct));

        // Create and post the original entry
        var createResult = await createHandler.Handle(new CreateJournalEntryCommand(
            tenantId,
            DateOnly.FromDateTime(DateTime.Today),
            "Original",
            null,
            new List<JournalLineInput>
            {
                new(asset.Id, 500m, 0m, null),
                new(income.Id, 0m, 500m, null)
            }
        ), CancellationToken.None);

        await postHandler.Handle(new PostJournalEntryCommand(tenantId, createResult.Value, null), CancellationToken.None);

        var reverseHandler = new ReverseJournalEntryHandler(ctx, mediatorMock.Object);
        var reverseResult = await reverseHandler.Handle(new ReverseJournalEntryCommand(
            tenantId,
            createResult.Value,
            DateOnly.FromDateTime(DateTime.Today),
            "Test reversal",
            null
        ), CancellationToken.None);

        reverseResult.IsSuccess.Should().BeTrue();

        var db = (TestFinanceDbContext)ctx;
        var original = db.JournalEntries.Find(createResult.Value)!;
        original.Status.Should().Be(EntryStatus.Reversed);
        original.ReversedByEntryId.Should().Be(reverseResult.Value);

        var reversal = db.JournalEntries.Include(e => e.Lines).First(e => e.Id == reverseResult.Value);
        var assetLine = reversal.Lines.First(l => l.AccountId == asset.Id);
        var incomeLine = reversal.Lines.First(l => l.AccountId == income.Id);

        // Debits and credits swapped vs original
        assetLine.Credit.Should().Be(500m);
        assetLine.Debit.Should().Be(0m);
        incomeLine.Debit.Should().Be(500m);
        incomeLine.Credit.Should().Be(0m);
    }
}
