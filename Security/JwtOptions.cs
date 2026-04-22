namespace nexusarena.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "nexusarena";
    public string Audience { get; set; } = "nexusarena-clients";
    public string SigningKey { get; set; } = "SUPER_SECRET_DEVELOPMENT_KEY_CHANGE_ME_123456789";
    public int ExpirationMinutes { get; set; } = 240;
}
