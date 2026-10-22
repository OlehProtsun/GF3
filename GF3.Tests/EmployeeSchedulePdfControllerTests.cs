using System.Security.Claims;
using BusinessLogicLayer.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WebApi.Auth;
using WebApi.Controllers;
using WebApi.Services;

namespace GF3.Tests;

public sealed class EmployeeSchedulePdfControllerTests
{
    [Fact]
    public async Task GetPdf_ReturnsAuthorizedAttachmentWithNoStore()
    {
        var exporter = new Exporter();
        var renderer = new Renderer();
        var controller = Create(exporter, renderer);
        var result = Assert.IsType<FileContentResult>(await controller.GetPdf(33, default));
        Assert.Equal("application/pdf", result.ContentType);
        Assert.StartsWith("GF3_Graph_33_", result.FileDownloadName);
        Assert.EndsWith(".pdf", result.FileDownloadName);
        Assert.Equal(renderer.Bytes, result.FileContents);
        Assert.Equal((7, 33), exporter.Request);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0")]
    [InlineData("invalid")]
    public async Task GetPdf_RejectsInvalidSession(string? claim)
        => await Assert.ThrowsAsync<BadHttpRequestException>(() => Create(new(), new(), claim).GetPdf(33, default));

    [Fact]
    public async Task GetPdf_PreservesNotFoundValidationAndCancellation()
    {
        foreach (var error in new Exception[] { new KeyNotFoundException("Schedule was not found."),
            new OperationCanceledException(), BusinessLogicLayer.Common.ValidationException.ForField("graphId", "Invalid") })
        {
            var controller = Create(new Exporter { Error = error }, new());
            var actual = await Record.ExceptionAsync(() => controller.GetPdf(33, default));
            Assert.Same(error, actual);
        }
        var rendererError = new OperationCanceledException();
        Assert.Same(rendererError, await Record.ExceptionAsync(() => Create(new(), new Renderer { Error = rendererError }).GetPdf(33, default)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetPdf_SanitizesExporterAndRendererFailures(bool exportFailure)
    {
        var error = new InvalidOperationException("private template path and data");
        var result = Assert.IsType<ObjectResult>(await Create(new Exporter { Error = exportFailure ? error : null },
            new Renderer { Error = exportFailure ? null : error }).GetPdf(33, default));
        Assert.Equal(500, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("pdf_generation_error", problem.Type);
        Assert.DoesNotContain("private", problem.Detail);
        Assert.Contains("application/problem+json", result.ContentTypes);
    }

    [Fact]
    public void Roles_KeepEmployeeAndManagerExportsSeparate()
    {
        Assert.Equal(AuthRoles.Employee, typeof(EmployeeSchedulePdfController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single().Roles);
        Assert.Equal(AuthRoles.Manager, typeof(ExportsController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Single().Roles);
    }

    private static EmployeeSchedulePdfController Create(Exporter exporter, Renderer renderer, string? claim = "7")
        => new(exporter, renderer, NullLogger<EmployeeSchedulePdfController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claim is null ? [] : [new Claim("employee_id", claim)], "test")),
            } },
        };

    private sealed class Exporter : IGraphTemplateExportService
    {
        public Exception? Error { get; init; }
        public (int, int) Request { get; private set; }
        public Task<(byte[] content, string fileName)> ExportPublishedGraphToXlsxAsync(int employeeId, int graphId, CancellationToken ct = default)
        {
            Request = (employeeId, graphId);
            return Error is null ? Task.FromResult((new byte[] { 1 }, "matrix.xlsx")) : Task.FromException<(byte[], string)>(Error);
        }
        public Task<(byte[] content, string fileName)> ExportGraphToXlsxAsync(int containerId, int graphId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
            => throw new NotSupportedException();
        public Task<(byte[] content, string fileName)> ExportContainerToXlsxAsync(int containerId, bool includeStyles, bool includeEmployees, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class Renderer : IScheduleWorksheetPdfRenderer
    {
        public byte[] Bytes { get; } = "%PDF-synthetic"u8.ToArray();
        public Exception? Error { get; init; }
        public byte[] Render(byte[] workbookBytes, CancellationToken ct = default) => Error is null ? Bytes : throw Error;
    }
}
