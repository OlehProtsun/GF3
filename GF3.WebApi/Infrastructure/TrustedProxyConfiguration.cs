using System.Net.Sockets;
using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace WebApi.Infrastructure;

public static class TrustedProxyConfiguration
{
    private static readonly IPNetwork[] PrivateNetworks =
        [IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("172.16.0.0/12"), IPNetwork.Parse("192.168.0.0/16")];

    public static void Configure(IServiceCollection services, IConfiguration configuration, bool isProduction)
    {
        if (!isProduction)
        {
            return;
        }

        var value = configuration["GF3_TRUSTED_PROXY_CIDR"];
        if (!IPNetwork.TryParse(value, out var network)
            || network.BaseAddress.AddressFamily != AddressFamily.InterNetwork
            || network.PrefixLength is < 16 or > 30
            || value != network.ToString()
            || !PrivateNetworks.Any(candidate => candidate.Contains(network.BaseAddress)))
        {
            throw new InvalidOperationException(
                "GF3_TRUSTED_PROXY_CIDR must be one canonical RFC1918 IPv4 CIDR (/16 through /30) matching the dedicated Docker bridge.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.KnownIPNetworks.Add(network);
        });
    }
}
