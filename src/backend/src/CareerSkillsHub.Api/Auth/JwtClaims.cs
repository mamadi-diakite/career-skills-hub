namespace CareerSkillsHub.Api.Auth;

public static class JwtClaims
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";

    /// <returns><c>null</c> when the principal has no valid user id.</returns>
    public static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(Subject), out var id) ? id : null;
}
