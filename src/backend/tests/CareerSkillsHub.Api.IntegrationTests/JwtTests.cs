namespace CareerSkillsHub.Api.IntegrationTests;

public sealed class JwtTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "issuer",
        Audience = "audience",
        AccessTokenLifetime = TimeSpan.FromMinutes(30),
        SigningKey = Convert.FromBase64String(CareerSkillsHubApiFactory.TestSigningKey),
    };

    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Reads_all_values_from_configuration()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(
            ("Issuer", "issuer"), ("Audience", "audience"), ("AccessTokenMinutes", "15"),
            ("SigningKey", CareerSkillsHubApiFactory.TestSigningKey)));

        Assert.Equal("issuer", settings.Issuer);
        Assert.Equal("audience", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15), settings.AccessTokenLifetime);
        Assert.Equal(Convert.FromBase64String(CareerSkillsHubApiFactory.TestSigningKey), settings.SigningKey);
    }

    [Fact]
    public void Uses_defaults_for_everything_but_the_key()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(("SigningKey", CareerSkillsHubApiFactory.TestSigningKey)));

        Assert.Equal("career-skills-hub", settings.Issuer);
        Assert.Equal("career-skills-hub", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(60), settings.AccessTokenLifetime);
    }

    [Theory]
    [InlineData(null, "Jwt:SigningKey is missing")]
    [InlineData(" ", "Jwt:SigningKey is missing")]
    [InlineData("not base64!", "must be a base64 string")]
    [InlineData("c2hvcnQ=", "at least 32 bytes")]
    public void Refuses_a_missing_or_weak_key(string? key, string message)
    {
        var configuration = key is null ? Configuration() : Configuration(("SigningKey", key));

        var error = Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(configuration));

        Assert.Contains(message, error.Message);
    }

    [Fact]
    public void Rejects_a_missing_configuration()
    {
        Assert.Throws<ArgumentNullException>(() => JwtSettings.FromConfiguration(null!));
    }

    [Fact]
    public void Validation_parameters_match_the_settings()
    {
        var parameters = Settings.ToValidationParameters();

        Assert.Equal(Settings.Issuer, parameters.ValidIssuer);
        Assert.Equal(Settings.Audience, parameters.ValidAudience);
        Assert.Equal(JwtClaims.Name, parameters.NameClaimType);
        Assert.Equal(JwtClaims.Role, parameters.RoleClaimType);
    }

    [Fact]
    public async Task Issued_token_is_valid_and_carries_the_user_claims()
    {
        var user = NewUser();
        var issuer = new JwtAccessTokenIssuer(Settings, TimeProvider.System);

        var token = issuer.Issue(user);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, Settings.ToValidationParameters());

        Assert.True(result.IsValid);
        Assert.Equal(user.Id.ToString(), result.ClaimsIdentity.FindFirst(JwtClaims.Subject)?.Value);
        Assert.Equal("marc.lefebvre@example.com", result.ClaimsIdentity.FindFirst(JwtClaims.Email)?.Value);
        Assert.Equal("Marc Lefebvre", result.ClaimsIdentity.Name);
        Assert.True(new ClaimsPrincipal(result.ClaimsIdentity).IsInRole("Manager"));
    }

    [Fact]
    public void Expiry_follows_the_configured_lifetime()
    {
        var token = new JwtAccessTokenIssuer(Settings, new FixedTimeProvider(Now)).Issue(NewUser());

        Assert.Equal(Now.UtcDateTime.AddMinutes(30), token.ExpiresAtUtc);
    }

    [Fact]
    public void Issuer_rejects_a_missing_user()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtAccessTokenIssuer(Settings, TimeProvider.System).Issue(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void User_id_is_null_without_a_valid_subject(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(JwtClaims.Subject, subject) };

        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity(claims)).GetUserId());
    }

    private static User NewUser() =>
        new(Guid.NewGuid(), "marc.lefebvre@example.com", "Marc Lefebvre", "hash", UserRole.Manager, Guid.NewGuid(), Now.UtcDateTime);

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>($"Jwt:{v.Key}", v.Value)))
            .Build();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
