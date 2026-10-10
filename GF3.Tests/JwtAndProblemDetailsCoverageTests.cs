using BusinessLogicLayer.Contracts.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Options;

namespace GF3.Tests;

public sealed class JwtAndProblemDetailsCoverageTests
{
    [Fact]
    public void JwtTokenService_CreateAccessToken_UsesConfiguredLifetimeAndSignedSessionClaims()
    {
        var options = new JwtAuthOptions
        {
            Issuer = "GF3.Tests",
            Audience = "GF3.FrontEnd.Tests",
            SigningKey = "GF3-tests-signing-key-with-enough-entropy",
            AccessTokenMinutes = 17,
        };
        var service = new JwtTokenService(Options.Create(options));
        var before = DateTimeOffset.UtcNow;

        var result = service.CreateAccessToken(new AuthenticatedSessionDto
        {
            UserName = "manager",
            DisplayName = "Main Manager",
            Role = AuthRoles.Manager,
            ManagerId = 9,
        });

        var after = DateTimeOffset.UtcNow;
        var expectedEarliestExpiration = before.AddMinutes(options.AccessTokenMinutes);
        var expectedLatestExpiration = after.AddMinutes(options.AccessTokenMinutes);
        var success = JwtTokenCodec.TryReadAccessToken(
            result.AccessToken,
            options,
            TimeSpan.FromSeconds(5),
            out var session,
            out var error);

        Assert.InRange(result.ExpiresAtUtc, expectedEarliestExpiration, expectedLatestExpiration);
        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("manager", session.UserName);
        Assert.Equal("Main Manager", session.DisplayName);
        Assert.Equal(AuthRoles.Manager, session.Role);
        Assert.Equal(9, session.ManagerId);
        Assert.Null(session.EmployeeId);
    }

    [Fact]
    public void ApiProblemDetailsFactory_CreateValidationProblem_UsesFirstValidationMessageAndTraceId()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/employees";
        context.TraceIdentifier = "trace-123";
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Name"] = ["Name is required."],
            ["Email"] = ["Email is invalid."],
        };

        var problem = ApiProblemDetailsFactory.CreateValidationProblem(context, errors);

        Assert.Equal("validation_error", problem.Type);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Name is required.", problem.Detail);
        Assert.Equal("/api/employees", problem.Instance);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.Equal(["Name is required."], problem.Errors["Name"]);
        Assert.Equal(["Email is invalid."], problem.Errors["Email"]);
    }

    [Fact]
    public void ApiProblemDetailsFactory_CreateValidationProblem_UsesFallbackWhenErrorsAreEmpty()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/shops";

        var problem = ApiProblemDetailsFactory.CreateValidationProblem(
            context,
            new Dictionary<string, string[]>(),
            "Fallback validation message.");

        Assert.Equal("Fallback validation message.", problem.Detail);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public void ApiProblemDetailsFactory_CreateProblem_MapsCommonProblemDetailsFields()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/containers/42";
        context.TraceIdentifier = "trace-456";

        var problem = ApiProblemDetailsFactory.CreateProblem(
            context,
            StatusCodes.Status409Conflict,
            "Conflict",
            "Container is locked.",
            "edit_lock_conflict");

        Assert.Equal("edit_lock_conflict", problem.Type);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Container is locked.", problem.Detail);
        Assert.Equal("/api/containers/42", problem.Instance);
        Assert.Equal("trace-456", problem.Extensions["traceId"]);
    }
}
