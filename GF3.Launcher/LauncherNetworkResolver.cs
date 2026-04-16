using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GF3.Launcher;

internal sealed record LauncherNetworkBinding(
    string ListenUrl,
    string LocalBaseUrl,
    IReadOnlyList<string> RemoteBaseUrls)
{
    public string? PreferredRemoteBaseUrl => RemoteBaseUrls.FirstOrDefault();
}

internal static class LauncherNetworkResolver
{
    private const string LocalhostAddress = "127.0.0.1";
    private const string AnyAddress = "0.0.0.0";

    public static LauncherNetworkBinding CreateBinding(int backendPort)
        => new(
            ListenUrl: $"http://{AnyAddress}:{backendPort}",
            LocalBaseUrl: $"http://{LocalhostAddress}:{backendPort}",
            RemoteBaseUrls: ResolveRemoteBaseUrls(backendPort));

    private static IReadOnlyList<string> ResolveRemoteBaseUrls(int backendPort)
    {
        var candidates = new List<(string Url, int Score)>();

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (!IsUsableNetworkInterface(networkInterface))
            {
                continue;
            }

            IPInterfaceProperties interfaceProperties;
            try
            {
                interfaceProperties = networkInterface.GetIPProperties();
            }
            catch
            {
                continue;
            }

            foreach (var addressInformation in interfaceProperties.UnicastAddresses)
            {
                var address = addressInformation.Address;
                if (!IsUsableAddress(address))
                {
                    continue;
                }

                candidates.Add((
                    Url: $"http://{address}:{backendPort}",
                    Score: ScoreAddress(address)));
            }
        }

        return candidates
            .OrderByDescending(static candidate => candidate.Score)
            .ThenBy(static candidate => candidate.Url, StringComparer.OrdinalIgnoreCase)
            .Select(static candidate => candidate.Url)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsUsableNetworkInterface(NetworkInterface networkInterface)
        => networkInterface.OperationalStatus == OperationalStatus.Up
            && networkInterface.NetworkInterfaceType is not NetworkInterfaceType.Loopback
            && networkInterface.NetworkInterfaceType is not NetworkInterfaceType.Tunnel;

    private static bool IsUsableAddress(IPAddress address)
    {
        if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
        {
            return false;
        }

        var bytes = address.GetAddressBytes();

        // Skip APIPA/link-local addresses because they are rarely useful for host access.
        if (bytes[0] == 169 && bytes[1] == 254)
        {
            return false;
        }

        return true;
    }

    private static int ScoreAddress(IPAddress address)
    {
        var bytes = address.GetAddressBytes();

        return bytes switch
        {
            [192, 168, ..] => 400,
            [172, >= 16 and <= 31, ..] => 350,
            [10, ..] => 300,
            [100, >= 64 and <= 127, ..] => 250,
            [127, ..] => 0,
            _ => 100,
        };
    }
}
