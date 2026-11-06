namespace CareerSkillsHub.Api.Auth;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public const int MinimumKeyBytes = 32;
    private const string DefaultIssuer = "career-skills-hub";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required TimeSpan AccessTokenLifetime { get; init; }

    public required byte[] SigningKey { get; init; }

    /// <remarks>
    /// The signing key is a base64 secret that never lives in the repository.
    /// The API refuses to start without it, in every environment.
    /// </remarks>
    public static JwtSettings FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(SectionName);

        return new JwtSettings
        {
            Issuer = section["Issuer"] ?? DefaultIssuer,
            Audience = section["Audience"] ?? DefaultIssuer,
            AccessTokenLifetime = TimeSpan.FromMinutes(section.GetValue("AccessTokenMinutes", 60)),
            SigningKey = ReadSigningKey(section["SigningKey"]),
        };
    }

    public TokenValidationParameters ToValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = new SymmetricSecurityKey(SigningKey),
        NameClaimType = JwtClaims.Name,
        RoleClaimType = JwtClaims.Role,
        ClockSkew = TimeSpan.FromSeconds(30),
    };

    private static byte[] ReadSigningKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing. Set it with an environment variable (Jwt__SigningKey) or user secrets.");
        }

        byte[] key;
        try
        {
            key = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be a base64 string.");
        }

        return key.Length >= MinimumKeyBytes
            ? key
            : throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinimumKeyBytes} bytes long.");
    }
}
