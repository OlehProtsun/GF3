using System.Text;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Regulations;
using BusinessLogicLayer.Services;
using DataAccessLayer.Repositories;
using GF3.Tests.Infrastructure;

namespace GF3.Tests;

public sealed class RegulationServiceTests
{
    [Fact]
    public async Task PublishAndAccept_PreservesVersionedAuditHistory()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new RegulationService(new RegulationRepository(context));
        var created = await service.CreateAsync(CreateRequest("2026.1"), 7, "Owner");

        Assert.Empty(await service.ListPendingAsync(CreateSubject()));

        var published = await service.PublishAsync(created.Id);
        var pending = await service.ListPendingAsync(CreateSubject());
        var accepted = await service.AcceptAsync(created.Id, CreateSubject());
        var acceptedAgain = await service.AcceptAsync(created.Id, CreateSubject());
        var history = await service.ListAcceptancesAsync("employee", 11);

        Assert.True(published.IsPublished);
        Assert.Single(pending);
        Assert.Equal(accepted.Id, acceptedAgain.Id);
        Assert.Single(history);
        Assert.Equal("2026.1", history[0].RegulationVersion);
        Assert.Equal(created.PdfSha256, history[0].PdfSha256);
        Assert.Empty(await service.ListPendingAsync(CreateSubject()));
        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Id, CreateRequest("2026.2")));
        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(created.Id));
    }

    [Fact]
    public async Task Create_RejectsInvalidPdfAndDuplicateVersion()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var service = new RegulationService(new RegulationRepository(context));

        await service.CreateAsync(CreateRequest("2026.1"), null, "Manager");
        var duplicate = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(CreateRequest("2026.1"), null, "Manager"));
        var invalid = CreateRequest("2026.2");
        invalid.PdfContent = Encoding.UTF8.GetBytes("not a pdf");
        var invalidPdf = await Assert.ThrowsAsync<ValidationException>(() =>
            service.CreateAsync(invalid, null, "Manager"));

        Assert.Contains("already exists", duplicate.Message);
        Assert.Contains("valid PDF", invalidPdf.Message);
    }

    private static SaveRegulationDocumentRequest CreateRequest(string version) => new()
    {
        Title = "Platform regulation",
        Version = version,
        Message = "Read and accept the attached document.",
        PdfFileName = $"regulation-{version}.pdf",
        PdfContent = Encoding.ASCII.GetBytes("%PDF-1.7 test document"),
    };

    private static RegulationSubject CreateSubject() => new()
    {
        Role = "employee",
        AccountId = 11,
        Username = "alice",
        DisplayName = "Alice Brown",
    };
}
