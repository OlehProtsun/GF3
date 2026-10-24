using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace WebApi.Options;

public sealed class PushOptions
{
    public bool Enabled { get; set; }
    public string? VapidPublicKey { get; set; }
    public string? VapidPrivateKey { get; set; }
    public string? VapidSubject { get; set; }
    public string[] AllowedEndpointHosts { get; set; } =
        ["fcm.googleapis.com", "updates.push.services.mozilla.com", "web.push.apple.com", "*.push.apple.com", "*.notify.windows.com"];

    public bool AllowsEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > 2048 ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length != 0 || !uri.IsDefaultPort || uri.Fragment.Length != 0 || uri.IsLoopback ||
            IPAddress.TryParse(uri.Host, out _) || !uri.Host.Contains('.') || uri.Host.EndsWith('.')) return false;
        return AllowedEndpointHosts.Any(host => host.StartsWith("*.", StringComparison.Ordinal)
            ? uri.Host.EndsWith(host[1..], StringComparison.OrdinalIgnoreCase) && uri.Host.Length > host.Length - 1
            : uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase));
    }

    public static byte[]? DecodeKey(string? value, int length)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 512 || !Regex.IsMatch(value, "^[A-Za-z0-9_-]+$")) return null;
        try
        {
            var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/').PadRight((value.Length + 3) / 4 * 4, '='));
            return bytes.Length == length ? bytes : null;
        }
        catch (FormatException) { return null; }
    }

    public bool IsValid()
    {
        if (!Enabled) return true;
        var publicKey = DecodeKey(VapidPublicKey, 65);
        var privateKey = DecodeKey(VapidPrivateKey, 32);
        if (publicKey is null || publicKey[0] != 4 || privateKey is null ||
            !Uri.TryCreate(VapidSubject, UriKind.Absolute, out var subject) ||
            !(subject.Scheme == "https" && subject.Host.Contains('.') && subject.UserInfo.Length == 0 ||
              subject.Scheme == "mailto" && System.Net.Mail.MailAddress.TryCreate(subject.OriginalString[7..], out var address) && address.Address == subject.OriginalString[7..])) return false;
        try
        {
            using var key = ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256, D = privateKey,
                Q = new ECPoint { X = publicKey[1..33], Y = publicKey[33..65] }
            });
            var message = new byte[] { 1, 2, 3 };
            return key.VerifyData(message, key.SignData(message, HashAlgorithmName.SHA256), HashAlgorithmName.SHA256);
        }
        catch (CryptographicException) { return false; }
    }
}
