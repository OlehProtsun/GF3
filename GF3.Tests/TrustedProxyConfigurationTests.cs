using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WebApi.Infrastructure;

namespace GF3.Tests;

public sealed class TrustedProxyConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("172.20.1.5/24")]
    [InlineData("172.20.1.0/24,172.20.2.0/24")]
    [InlineData("8.8.8.0/24")]
    [InlineData("0.0.0.0/0")]
    [InlineData("fd12::/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("192.168.0.1/32")]
    public void InvalidProductionNetworkFailsAtConfiguration(string? cidr)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            TrustedProxyConfiguration.Configure(new ServiceCollection(), Configuration(cidr), isProduction: true));
        Assert.Contains("GF3_TRUSTED_PROXY_CIDR", error.Message);
    }

    [Theory]
    [InlineData("172.20.0.0/16")]
    [InlineData("172.20.1.0/24")]
    public void ExactlyOnePrivateNetworkIsTrusted(string cidr)
    {
        var services = new ServiceCollection();
        TrustedProxyConfiguration.Configure(services, Configuration(cidr), isProduction: true);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        Assert.Equal(cidr, Assert.Single(options.KnownIPNetworks).ToString());
        Assert.Empty(options.KnownProxies);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
    }

    [Theory]
    [InlineData("172.20.1.2", "203.0.113.3", "https", "203.0.113.3", "https")]
    [InlineData("172.20.1.2", "203.0.113.4, 203.0.113.3", "http, https", "203.0.113.3", "https")]
    [InlineData("172.20.2.2", "203.0.113.4, 203.0.113.3", "http, https", "172.20.2.2", "http")]
    [InlineData("127.0.0.1", "203.0.113.3", "https", "127.0.0.1", "http")]
    public async Task MiddlewareOnlyConsumesTheVerifiedHop(
        string peer, string forwardedFor, string forwardedProto, string expectedIp, string expectedScheme)
    {
        var services = new ServiceCollection();
        TrustedProxyConfiguration.Configure(services, Configuration("172.20.1.0/24"), isProduction: true);
        using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("app.example.test");
        context.Request.Headers["X-Forwarded-For"] = forwardedFor;
        context.Request.Headers["X-Forwarded-Proto"] = forwardedProto;
        context.Request.Headers["X-Forwarded-Host"] = "other.example.test";

        var middleware = new ForwardedHeadersMiddleware(next =>
        {
            Assert.Equal(IPAddress.Parse(expectedIp), next.Connection.RemoteIpAddress);
            Assert.Equal(expectedScheme, next.Request.Scheme);
            Assert.Equal("app.example.test", next.Request.Host.Value);
            return Task.CompletedTask;
        }, NullLoggerFactory.Instance, provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>());
        await middleware.Invoke(context);
    }

    [Fact]
    public void DevelopmentKeepsExistingConfiguration()
    {
        var services = new ServiceCollection();
        TrustedProxyConfiguration.Configure(services, Configuration(null), isProduction: false);
        Assert.Empty(services);
    }

    private static IConfiguration Configuration(string? cidr) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GF3_TRUSTED_PROXY_CIDR"] = cidr,
        }).Build();
}
