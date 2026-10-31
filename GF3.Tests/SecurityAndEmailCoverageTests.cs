using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Common;
using BusinessLogicLayer.Contracts.Auth;
using BusinessLogicLayer.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebApi.Auth;
using WebApi.Infrastructure;
using WebApi.Options;

namespace GF3.Tests;

public sealed class SecurityAndEmailCoverageTests
{
    [Fact]
    public void PasswordHasher_HashesVerifyAndUseFreshSalt()
    {
        var hasher = new PasswordHasher();

        var firstHash = hasher.HashPassword("correct horse battery staple");
        var secondHash = hasher.HashPassword("correct horse battery staple");

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(hasher.VerifyPassword("correct horse battery staple", firstHash));
        Assert.True(hasher.VerifyPassword("correct horse battery staple", secondHash));
        Assert.False(hasher.VerifyPassword("wrong", firstHash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2-sha256$bad-iterations$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$100000$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha256$100000$not-base64$aGFzaA==")]
    public void PasswordHasher_ReturnsFalseForMalformedHashes(string passwordHash)
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.VerifyPassword("password", passwordHash));
    }

    [Fact]
    public void PasswordHasher_RejectsBlankPasswordWhenHashing()
    {
        var hasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(() => hasher.HashPassword(" "));
    }

    [Theory]
    [InlineData("", "sender@example.com")]
    [InlineData("smtp.example.com", "")]
    public async Task SmtpEmailSender_ThrowsValidation_WhenSmtpIsNotConfigured(string host, string fromAddress)
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpEmailOptions
        {
            Host = host,
            FromAddress = fromAddress,
            FromDisplayName = "GF3",
        }));

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.SendAsync("worker@example.com", "Worker", "Subject", "Body"));

        Assert.Equal(
            "Email delivery is not configured yet. Add SMTP settings before using password recovery.",
            exception.Message);
    }

    [Theory]
    [InlineData("not-an-email", "recipient@example.com")]
    [InlineData("sender@example.com", "bad recipient")]
    public async Task SmtpEmailSender_ValidatesMailAddressesBeforeNetworkSend(
        string fromAddress,
        string toAddress)
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpEmailOptions
        {
            Host = "smtp.example.com",
            FromAddress = fromAddress,
            FromDisplayName = "GF3",
            UserName = " smtp-user ",
            Password = "smtp-password",
            Port = 0,
            EnableSsl = true,
        }));

        await Assert.ThrowsAsync<FormatException>(() =>
            sender.SendAsync(toAddress, "Worker", "Subject", "Body"));
    }

    [Fact]
    public void JwtTokenCodec_RoundTripsManagerSessionAndUsesUserNameAsDisplayFallback()
    {
        var options = CreateJwtOptions();
        var token = WriteSignedToken(
            new { alg = "HS256", typ = "JWT" },
            new
            {
                iss = options.Issuer,
                aud = options.Audience,
                sub = "manager",
                role = AuthRoles.Manager,
                manager_id = "5",
                iat = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
                nbf = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
                exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
            },
            options.SigningKey);

        var success = JwtTokenCodec.TryReadAccessToken(
            token,
            options,
            TimeSpan.Zero,
            out var session,
            out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal("manager", session.UserName);
        Assert.Equal("manager", session.DisplayName);
        Assert.Equal(AuthRoles.Manager, session.Role);
        Assert.Equal(5, session.ManagerId);
        Assert.Null(session.EmployeeId);
    }

    [Fact]
    public async Task JwtAuthenticationHandler_AuthenticatesBearerHeaderAndBuildsEmployeeClaims()
    {
        var options = CreateJwtOptions();
        var token = JwtTokenCodec.WriteAccessToken(
            options,
            new AuthenticatedSessionDto
            {
                UserName = "worker",
                Role = AuthRoles.Employee,
                DisplayName = "Worker Bee",
                EmployeeId = 42,
            },
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(10));

        var result = await AuthenticateRequestAsync(options, context =>
        {
            context.Request.Headers.Authorization = $"Bearer {token}";
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("worker", result.Principal!.Identity?.Name);
        Assert.True(result.Principal.IsInRole(AuthRoles.Employee));
        Assert.Equal("Worker Bee", result.Principal.FindFirst("display_name")?.Value);
        Assert.Equal("42", result.Principal.FindFirst("employee_id")?.Value);
        Assert.Null(result.Principal.FindFirst("manager_id"));
    }

    [Fact]
    public async Task JwtAuthenticationHandler_UsesQueryTokenWhenBearerHeaderIsBlank()
    {
        var options = CreateJwtOptions();
        var token = JwtTokenCodec.WriteAccessToken(
            options,
            new AuthenticatedSessionDto
            {
                UserName = "manager",
                Role = AuthRoles.Employee,
                DisplayName = "Main Manager",
                EmployeeId = 7,
            },
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(10));

        var result = await AuthenticateRequestAsync(options, context =>
        {
            context.Request.Path = WebApi.Realtime.EmployeePresenceHub.RoutePattern;
            context.Request.Headers.Authorization = "Bearer   ";
            context.Request.QueryString = QueryString.Create("access_token", token);
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.Equal("manager", result.Principal!.Identity?.Name);
        Assert.True(result.Principal.IsInRole(AuthRoles.Employee));
        Assert.Equal("7", result.Principal.FindFirst("employee_id")?.Value);
        Assert.Null(result.Principal.FindFirst("manager_id"));
    }

    [Fact]
    public async Task JwtAuthenticationHandler_ReturnsNoResultWithoutTokenAndFailureForInvalidToken()
    {
        var options = CreateJwtOptions();

        var missing = await AuthenticateRequestAsync(options, _ => { });
        var invalid = await AuthenticateRequestAsync(options, context =>
        {
            context.Request.Headers.Authorization = "Bearer too.few";
        });

        Assert.False(missing.Succeeded);
        Assert.Null(missing.Failure);
        Assert.Null(missing.Principal);

        Assert.False(invalid.Succeeded);
        Assert.NotNull(invalid.Failure);
        Assert.Equal("Token format is invalid.", invalid.Failure!.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidJwtCases))]
    public void JwtTokenCodec_ReturnsExpectedErrorsForInvalidTokens(
        Func<JwtAuthOptions, string> tokenFactory,
        string expectedError)
    {
        var options = CreateJwtOptions();

        var success = JwtTokenCodec.TryReadAccessToken(
            tokenFactory(options),
            options,
            TimeSpan.Zero,
            out _,
            out var error);

        Assert.False(success);
        Assert.Equal(expectedError, error);
    }

    public static IEnumerable<object[]> InvalidJwtCases()
    {
        yield return [new Func<JwtAuthOptions, string>(_ => "too.few"), "Token format is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => $"not-json.{Base64UrlEncodeJson(new { })}.abc"), "Token header is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "none" }, ValidPayload(options), options.SigningKey)), "Token algorithm is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => $"{Base64UrlEncodeJson(new { alg = "HS256" })}.{Base64UrlEncodeJson(ValidPayload(options))}.###"), "Token signature is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, ValidPayload(options), "wrong-key")), "Token signature check failed."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, "not-json", options.SigningKey, payloadIsRawText: true)), "Token payload is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, ValidPayload(options) with { iss = "other" }, options.SigningKey)), "Token issuer is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, ValidPayload(options) with { aud = "other" }, options.SigningKey)), "Token audience is invalid."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, new { iss = options.Issuer, aud = options.Audience, sub = "worker", role = AuthRoles.Employee }, options.SigningKey)), "Token expiration is missing."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, ValidPayload(options) with { exp = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds() }, options.SigningKey)), "Token expired."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, ValidPayload(options) with { nbf = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds() }, options.SigningKey)), "Token is not active yet."];
        yield return [new Func<JwtAuthOptions, string>(options => WriteSignedToken(new { alg = "HS256" }, ValidPayload(options) with { sub = "", name = "" }, options.SigningKey)), "Token is missing required claims."];
    }

    private static JwtPayload ValidPayload(JwtAuthOptions options)
        => new(
            options.Issuer,
            options.Audience,
            "worker",
            "worker",
            AuthRoles.Employee,
            "Worker",
            DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());

    private sealed record JwtPayload(
        string iss,
        string aud,
        string sub,
        string name,
        string role,
        string display_name,
        long iat,
        long nbf,
        long exp);

    private static JwtAuthOptions CreateJwtOptions() => new()
    {
        Issuer = "GF3.Tests",
        Audience = "GF3.FrontEnd.Tests",
        SigningKey = "GF3-tests-signing-key-with-enough-entropy",
        AccessTokenMinutes = 30,
    };

    private static async Task<AuthenticateResult> AuthenticateRequestAsync(
        JwtAuthOptions jwtOptions,
        Action<HttpContext> configureRequest)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Options.Create(jwtOptions));
        services
            .AddAuthentication(JwtAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, JwtAuthenticationHandler>(
                JwtAuthenticationDefaults.SchemeName,
                _ => { });

        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
        };
        configureRequest(context);

        return await context.AuthenticateAsync(JwtAuthenticationDefaults.SchemeName);
    }

    private static string WriteSignedToken(object header, object payload, string signingKey, bool payloadIsRawText = false)
    {
        var headerSegment = Base64UrlEncodeJson(header);
        var payloadSegment = payloadIsRawText
            ? Base64UrlEncode(Encoding.UTF8.GetBytes((string)payload))
            : Base64UrlEncodeJson(payload);
        var signingInput = $"{headerSegment}.{payloadSegment}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signature = Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput)));
        return $"{signingInput}.{signature}";
    }

    private static string Base64UrlEncodeJson(object value)
        => Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(value));

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
