using Microsoft.Extensions.Configuration;

namespace WebApi.Options;

/// <summary>
/// JWT settings used by the API to issue and validate bearer tokens.
/// </summary>
public sealed class JwtAuthOptions
{
    private const string FallbackDevelopmentSigningKey = "GF3-local-dev-jwt-signing-key-change-before-production-2026-04-22";

    public string Issuer { get; set; } = "GF3.WebApi";

    public string Audience { get; set; } = "GF3.FrontEnd";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 720;

    public string GetEffectiveSigningKey()
        => string.IsNullOrWhiteSpace(SigningKey) ? FallbackDevelopmentSigningKey : SigningKey.Trim();

    public static JwtAuthOptions FromConfiguration(IConfiguration configuration)
    {
        var options = new JwtAuthOptions();
        configuration.GetSection("Jwt").Bind(options);

        options.Issuer = string.IsNullOrWhiteSpace(options.Issuer) ? "GF3.WebApi" : options.Issuer.Trim();
        options.Audience = string.IsNullOrWhiteSpace(options.Audience) ? "GF3.FrontEnd" : options.Audience.Trim();
        options.AccessTokenMinutes = options.AccessTokenMinutes <= 0 ? 720 : options.AccessTokenMinutes;
        options.SigningKey = options.GetEffectiveSigningKey();

        return options;
    }
}
