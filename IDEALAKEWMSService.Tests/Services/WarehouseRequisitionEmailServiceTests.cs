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
        var svc = new WarehouseRequisitionEmailService(ctx, repo, mail.Object, config, NullLogger<WarehouseRequisitionEmailService>.Instance);
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
        mail.Setup(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, IEnumerable<string>, CancellationToken>((s, b, _, _) => capturedBody = b)
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
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
