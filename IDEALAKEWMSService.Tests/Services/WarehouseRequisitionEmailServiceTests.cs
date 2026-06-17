using FluentAssertions;
using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Tests.Helpers;
using IDEALAKEWMSService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IDEALAKEWMSService.Tests.Services;

public class WarehouseRequisitionEmailServiceTests
{
    private static (WarehouseRequisitionEmailService svc, ApplicationDbContext ctx, IWarehouseRequisitionRepository repo, Mock<IMailService> mail) Setup()
    {
        var ctx = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(ctx);
        var mail = new Mock<IMailService>();
        var config = new ConfigurationBuilder().Build();
        var svc = new WarehouseRequisitionEmailService(ctx, repo, mail.Object, config, NullLogger<WarehouseRequisitionEmailService>.Instance, new FakeSyncLogger());
        return (svc, ctx, repo, mail);
    }

    private static async Task<WarehouseRequisition> SeedSubmittedAsync(ApplicationDbContext ctx, IWarehouseRequisitionRepository repo)
    {
        var u = new User { Name = "tester", IsActive = true, CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var wp = new ProductionWorkplace { Name = "WB-A", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        var grp = new OrderRecipientGroup { Name = "Lager", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
        ctx.Users.Add(u); ctx.ProductionWorkplaces.Add(wp); ctx.OrderRecipientGroups.Add(grp);
        await ctx.SaveChangesAsync();
        ctx.OrderRecipients.Add(new OrderRecipient
        {
            OrderRecipientGroupId = grp.Id, Name = "Lager-Team", Email = "lager@ake.at", IsActive = true,
            CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
        });
        await ctx.SaveChangesAsync();

        var id = await repo.CreateDraftAsync(wp.Id, u.Id, "tester", "DOMAIN\\tester");
        await repo.AddItemAsync(id, "ART-1", "Schraube", "Stk", 5m, "tester", "DOMAIN\\tester");
        var r = await ctx.WarehouseRequisitions.FindAsync(id);
        await repo.SubmitAsync(id, grp.Id, u.Id, "tester", "DOMAIN\\tester", r!.RowVersion);
        return (await ctx.WarehouseRequisitions.FindAsync(id))!;
    }

    [Fact]
    public async Task SendPending_SubmittedWithoutEmail_TriggersOneMail()
    {
        var (svc, ctx, repo, mail) = Setup();
        var r = await SeedSubmittedAsync(ctx, repo);

        var result = await svc.SendPendingEmailsAsync(dryRun: false);

        result.SubmitsSent.Should().Be(1);
        mail.Verify(m => m.SendAsync(
            It.Is<string>(s => s.Contains($"#{r.Id}") && s.Contains("WB-A")),
            It.IsAny<string>(),
            It.Is<IEnumerable<string>>(e => e.Contains("lager@ake.at")),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
        var updated = await ctx.WarehouseRequisitions.FindAsync(r.Id);
        updated!.EmailSentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SendPending_BodyContainsItemsAndSubject()
    {
        var (svc, ctx, repo, mail) = Setup();
        await SeedSubmittedAsync(ctx, repo);
        string? capturedBody = null;
        mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, string?, CancellationToken>((s, b, _, _, _) => capturedBody = b)
            .Returns(Task.CompletedTask);

        await svc.SendPendingEmailsAsync(dryRun: false);

        capturedBody.Should().NotBeNull();
        capturedBody!.Should().Contain("ART-1");
        capturedBody.Should().Contain("Schraube");
    }

    [Fact]
    public async Task SendPending_DryRun_DoesNotMarkEmailSent()
    {
        var (svc, ctx, repo, mail) = Setup();
        var r = await SeedSubmittedAsync(ctx, repo);

        await svc.SendPendingEmailsAsync(dryRun: true);

        var updated = await ctx.WarehouseRequisitions.FindAsync(r.Id);
        updated!.EmailSentAt.Should().BeNull();
    }

    [Fact]
    public async Task SendPending_CancelledWithoutPriorSubmitMail_NoCancellationMail()
    {
        var (svc, ctx, repo, mail) = Setup();
        var r = await SeedSubmittedAsync(ctx, repo);
        // Direkt cancel ohne dass Submit-Mail je gesendet wurde (EmailSentAt null)
        await repo.CancelAsync(r.Id, "Falsch", 0, "t", "t", r.RowVersion);

        var result = await svc.SendPendingEmailsAsync(dryRun: false);

        result.CancellationsSent.Should().Be(0, "Storno-Mail nur wenn Submit-Mail vorher rausging");
    }

    [Fact]
    public async Task SendPending_CancelledAfterSubmit_TriggersStornoMail()
    {
        var (svc, ctx, repo, mail) = Setup();
        var r = await SeedSubmittedAsync(ctx, repo);
        // erst Submit-Mail rausgehen lassen
        await svc.SendPendingEmailsAsync(dryRun: false);
        // dann cancel
        var rAfter = await ctx.WarehouseRequisitions.FindAsync(r.Id);
        await repo.CancelAsync(r.Id, "Falsch erfasst", 0, "t", "t", rAfter!.RowVersion);

        var result = await svc.SendPendingEmailsAsync(dryRun: false);

        result.CancellationsSent.Should().Be(1);
        mail.Verify(m => m.SendAsync(
            It.Is<string>(s => s.StartsWith("[STORNO]")),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Send_writes_lifecycle_to_synclogger()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(ctx);
        var mailMock = new Mock<IMailService>();
        var config = new ConfigurationBuilder().Build();
        var fakeLogger = new FakeSyncLogger();
        var service = new WarehouseRequisitionEmailService(
            ctx, repo, mailMock.Object, config,
            NullLogger<WarehouseRequisitionEmailService>.Instance, fakeLogger);

        await service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

        fakeLogger.Runs.Should().ContainSingle();
        fakeLogger.Runs[0].ServiceName.Should().Be("WarehouseRequisitionEmail");
        fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Send_uses_submit_and_storno_reference_prefixes()
    {
        using var ctx = TestDbContextFactory.Create();
        var repo = new WarehouseRequisitionRepository(ctx);
        var mailMock = new Mock<IMailService>();
        mailMock.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var config = new ConfigurationBuilder().Build();
        var fakeLogger = new FakeSyncLogger();
        var service = new WarehouseRequisitionEmailService(
            ctx, repo, mailMock.Object, config,
            NullLogger<WarehouseRequisitionEmailService>.Instance, fakeLogger);

        // Seed one pending submit
        var submitReq = await SeedSubmittedAsync(ctx, repo);

        // Send to mark email as sent (sets EmailSentAt)
        await service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

        // Now cancel so there is a pending storno
        var rAfter = await ctx.WarehouseRequisitions.FindAsync(submitReq.Id);
        await repo.CancelAsync(submitReq.Id, "Test-Storno", 0, "t", "t", rAfter!.RowVersion);

        // Reset FakeSyncLogger state by creating a fresh one for the second call
        var fakeLogger2 = new FakeSyncLogger();
        var service2 = new WarehouseRequisitionEmailService(
            ctx, repo, mailMock.Object, config,
            NullLogger<WarehouseRequisitionEmailService>.Instance, fakeLogger2);

        // Seed a second pending submit (different request) for the submit prefix
        var submitReq2 = await SeedSubmittedAsync(ctx, repo);

        await service2.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);

        var events = fakeLogger2.Runs[0].Events;
        events.Should().Contain(e => e.Reference != null && e.Reference.StartsWith("submit/"));
        events.Should().Contain(e => e.Reference != null && e.Reference.StartsWith("storno/"));
    }

    [Fact]
    public void BuildSubmitText_ContainsNakedLinkAndItems()
    {
        var r = new WarehouseRequisition
        {
            Id = 42,
            ProductionWorkplace = new ProductionWorkplace { Name = "WB-A" },
            CreatedBy = "tester",
            SubmittedAt = new DateTime(2026, 6, 16, 8, 0, 0),
            Items =
            {
                new WarehouseRequisitionItem { Position = 1, ArticleNumber = "ART-1", ArticleDescription = "Schraube", Unit = "Stk", QuantityRequested = 5m },
                new WarehouseRequisitionItem { Position = 2, ArticleNumber = "ART-2", ArticleDescription = "Mutter", Unit = "Stk", QuantityRequested = 3m },
            }
        };

        var text = WarehouseRequisitionEmailService.BuildSubmitText(r, "https://wms.ake.at");

        // Nackte, klickbare URL — kein Markdown-/HTML-Markup
        text.Should().Contain("Lagerbestellung oeffnen: https://wms.ake.at/WarehousePicking/Details/42");
        text.Should().NotContain("<a");
        text.Should().NotContain("[");
        text.Should().NotContain("]");
        // Positionsdaten
        text.Should().Contain("ART-1");
        text.Should().Contain("Schraube");
        text.Should().Contain("ART-2");
        text.Should().Contain("WB-A");
    }
}
