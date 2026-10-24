using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using BusinessLogicLayer.Contracts.Auth;
using WebApi.Auth;
using WebApi.Options;

namespace GF3.Tests;

public sealed class ManagerWorkspaceJwtTests
{
    private static readonly JwtAuthOptions Options = new() { Issuer = "tests", Audience = "tests", SigningKey = "synthetic-mode-tests-signing-key-2026" };
    private static string Token(string? mode, string role = AuthRoles.Manager) => JwtTokenCodec.WriteAccessToken(Options, new AuthenticatedSessionDto { Role = role, UserName = "user", ManagerId = role == AuthRoles.Manager ? 1 : null, CredentialVersion = 123, WorkspaceMode = mode }, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5));
    [Theory]
    [InlineData("choose", "choose")]
    [InlineData("pc", "pc")]
    [InlineData("phone", "phone")]
    [InlineData(null, "pc")]
    public void SignedModeRoundTripsAndLegacyRemainsPc(string? mode, string expected)
    {
        Assert.True(JwtTokenCodec.TryReadAccessToken(Token(mode), Options, TimeSpan.Zero, out var session, out _));
        Assert.Equal(expected, session.WorkspaceMode); Assert.Equal(123, session.CredentialVersion);
    }
    [Fact]
    public void EmployeeWithoutModeIsPreserved()
    {
        Assert.True(JwtTokenCodec.TryReadAccessToken(Token(null, AuthRoles.Employee), Options, TimeSpan.Zero, out var session, out _));
        Assert.Null(session.WorkspaceMode); Assert.Equal(AuthRoles.Employee, session.Role);
    }
    [Theory]
    [InlineData("manager", "invalid")]
    [InlineData("manager", "")]
    [InlineData("employee", "phone")]
    public void InvalidExplicitSignedClaimsAreRejected(string role, string mode)
    {
        var token = Rewrite(Token(null, role), payload => payload[ManagerWorkspaceModes.ClaimType] = mode);
        Assert.False(JwtTokenCodec.TryReadAccessToken(token, Options, TimeSpan.Zero, out _, out var error));
        Assert.Contains("workspace mode", error);
    }
    [Fact]
    public void NullOrNumericExplicitClaimsAreRejected()
    {
        foreach (var value in new JsonNode?[] { null, JsonValue.Create(1) })
        {
            var token = Rewrite(Token(null), payload => payload[ManagerWorkspaceModes.ClaimType] = value);
            Assert.False(JwtTokenCodec.TryReadAccessToken(token, Options, TimeSpan.Zero, out _, out _));
        }
        Assert.Throws<ArgumentException>(() => Token("invalid"));
    }
    internal static string Rewrite(string token, Action<JsonObject> mutate)
    {
        var parts = token.Split('.');
        var json = JsonNode.Parse(Convert.FromBase64String(parts[1].Replace('-', '+').Replace('_', '/').PadRight((parts[1].Length + 3) / 4 * 4, '=')))!.AsObject();
        mutate(json);
        parts[1] = Encode(Encoding.UTF8.GetBytes(json.ToJsonString()));
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Options.SigningKey));
        parts[2] = Encode(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}")));
        return string.Join('.', parts);
    }
    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
