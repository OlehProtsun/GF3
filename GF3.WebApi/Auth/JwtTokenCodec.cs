using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusinessLogicLayer.Contracts.Auth;
using WebApi.Options;

namespace WebApi.Auth;

/// <summary>
/// Minimal HS256 JWT encoder/decoder used to keep authentication self-contained in the project.
/// </summary>
internal static class JwtTokenCodec
{
    public static string WriteAccessToken(
        JwtAuthOptions options,
        AuthenticatedSessionDto session,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var header = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT",
        });

        var payload = JsonSerializer.SerializeToUtf8Bytes(CreatePayload(options, session, issuedAtUtc, expiresAtUtc));
        var headerSegment = Base64UrlEncode(header);
        var payloadSegment = Base64UrlEncode(payload);
        var signingInput = $"{headerSegment}.{payloadSegment}";
        var signatureSegment = Base64UrlEncode(ComputeSignature(signingInput, options.SigningKey));

        return $"{signingInput}.{signatureSegment}";
    }

    public static bool TryReadAccessToken(
        string token,
        JwtAuthOptions options,
        TimeSpan clockSkew,
        out AuthenticatedSessionDto session,
        out string? error)
        => TryReadAccessToken(token, options, clockSkew, out session, out error, out _);

    public static bool TryReadAccessToken(
        string token,
        JwtAuthOptions options,
        TimeSpan clockSkew,
        out AuthenticatedSessionDto session,
        out string? error,
        out DateTimeOffset expiresAtUtc)
    {
        session = new AuthenticatedSessionDto();
        error = null;
        expiresAtUtc = default;

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            error = "Token format is invalid.";
            return false;
        }

        if (!TryDecodeJson(parts[0], out var headerRoot))
        {
            error = "Token header is invalid.";
            return false;
        }

        using (headerRoot)
        {
            var algorithm = ReadString(headerRoot.RootElement, "alg");
            if (!string.Equals(algorithm, "HS256", StringComparison.Ordinal))
            {
                error = "Token algorithm is invalid.";
                return false;
            }
        }

        if (!TryDecodeBase64Url(parts[2], out var signature))
        {
            error = "Token signature is invalid.";
            return false;
        }

        var expectedSignature = ComputeSignature($"{parts[0]}.{parts[1]}", options.SigningKey);
        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
        {
            error = "Token signature check failed.";
            return false;
        }

        if (!TryDecodeJson(parts[1], out var payloadRoot))
        {
            error = "Token payload is invalid.";
            return false;
        }

        using (payloadRoot)
        {
            if (!string.Equals(ReadString(payloadRoot.RootElement, "iss"), options.Issuer, StringComparison.Ordinal))
            {
                error = "Token issuer is invalid.";
                return false;
            }

            if (!string.Equals(ReadString(payloadRoot.RootElement, "aud"), options.Audience, StringComparison.Ordinal))
            {
                error = "Token audience is invalid.";
                return false;
            }

            var nowUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var clockSkewSeconds = (long)Math.Max(0, clockSkew.TotalSeconds);
            var expiresAtUnixTime = ReadLong(payloadRoot.RootElement, "exp");
            if (!expiresAtUnixTime.HasValue)
            {
                error = "Token expiration is missing.";
                return false;
            }

            if (expiresAtUnixTime.Value < DateTimeOffset.MinValue.ToUnixTimeSeconds() ||
                expiresAtUnixTime.Value > DateTimeOffset.MaxValue.ToUnixTimeSeconds())
            {
                error = "Token expiration is invalid.";
                return false;
            }
            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnixTime.Value);

            if (nowUnixTime >= expiresAtUnixTime.Value + clockSkewSeconds)
            {
                error = "Token expired.";
                return false;
            }

            var notBeforeUnixTime = ReadLong(payloadRoot.RootElement, "nbf");
            if (notBeforeUnixTime.HasValue && nowUnixTime + clockSkewSeconds < notBeforeUnixTime.Value)
            {
                error = "Token is not active yet.";
                return false;
            }

            var userName = ReadString(payloadRoot.RootElement, "name") ?? ReadString(payloadRoot.RootElement, "sub");
            var role = ReadString(payloadRoot.RootElement, "role");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(role))
            {
                error = "Token is missing required claims.";
                return false;
            }

            var hasMode = payloadRoot.RootElement.TryGetProperty(ManagerWorkspaceModes.ClaimType, out var modeProperty);
            var mode = hasMode && modeProperty.ValueKind == JsonValueKind.String ? modeProperty.GetString() : null;
            if (hasMode && (role != AuthRoles.Manager || mode is not (ManagerWorkspaceModes.Choose or ManagerWorkspaceModes.Pc or ManagerWorkspaceModes.Phone)))
            {
                error = "Token manager workspace mode is invalid.";
                return false;
            }

            session = new AuthenticatedSessionDto
            {
                WorkspaceMode = role == AuthRoles.Manager ? ManagerWorkspaceModes.ResolveForManager(mode) : null,
                UserName = userName,
                Role = role,
                DisplayName = ReadString(payloadRoot.RootElement, "display_name") ?? userName,
                ManagerId = ReadNullableInt(payloadRoot.RootElement, "manager_id"),
                EmployeeId = ReadNullableInt(payloadRoot.RootElement, "employee_id"),
                SessionVersion = ReadNullableInt(payloadRoot.RootElement, "session_version"),
                CredentialVersion = ReadLong(payloadRoot.RootElement, "credential_version"),
            };

            return true;
        }
    }

    private static Dictionary<string, object> CreatePayload(
        JwtAuthOptions options,
        AuthenticatedSessionDto session,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var payload = new Dictionary<string, object>
        {
            ["iss"] = options.Issuer,
            ["aud"] = options.Audience,
            ["sub"] = session.UserName,
            ["name"] = session.UserName,
            ["role"] = session.Role,
            ["display_name"] = session.DisplayName,
            ["iat"] = issuedAtUtc.ToUnixTimeSeconds(),
            ["nbf"] = issuedAtUtc.ToUnixTimeSeconds(),
            ["exp"] = expiresAtUtc.ToUnixTimeSeconds(),
        };

        if (session.EmployeeId.HasValue)
        {
            payload["employee_id"] = session.EmployeeId.Value;
            payload["session_version"] = session.SessionVersion ?? 0;
        }

        if (session.ManagerId.HasValue)
        {
            payload["manager_id"] = session.ManagerId.Value;
            if (session.CredentialVersion.HasValue)
            {
                payload["credential_version"] = session.CredentialVersion.Value;
            }
        }

        if (session.Role == AuthRoles.Manager && session.WorkspaceMode is not null)
        {
            payload[ManagerWorkspaceModes.ClaimType] = ManagerWorkspaceModes.ResolveForManager(session.WorkspaceMode);
        }

        return payload;
    }

    private static byte[] ComputeSignature(string signingInput, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
    }

    private static bool TryDecodeJson(string segment, out JsonDocument document)
    {
        document = null!;
        if (!TryDecodeBase64Url(segment, out var bytes))
        {
            return false;
        }

        try
        {
            document = JsonDocument.Parse(bytes);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryDecodeBase64Url(string input, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        var padded = input
            .Replace('-', '+')
            .Replace('_', '/');

        var remainder = padded.Length % 4;
        if (remainder is > 0)
        {
            padded = padded.PadRight(padded.Length + (4 - remainder), '=');
        }

        try
        {
            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.GetRawText(),
            _ => null,
        };
    }

    private static long? ReadLong(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out number))
        {
            return number;
        }

        return null;
    }

    private static int? ReadNullableInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out number))
        {
            return number;
        }

        return null;
    }
}
