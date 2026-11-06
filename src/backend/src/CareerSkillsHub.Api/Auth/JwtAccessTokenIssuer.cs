namespace CareerSkillsHub.Api.Auth;

/// <remarks>The role claim is informative only: access rules read the role from the database on every request.</remarks>
public sealed class JwtAccessTokenIssuer(JwtSettings settings, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now + settings.AccessTokenLifetime;

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [JwtClaims.Subject] = user.Id.ToString(),
                [JwtClaims.Email] = user.Email,
                [JwtClaims.Name] = user.DisplayName,
                [JwtClaims.Role] = user.Role.ToString(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}
