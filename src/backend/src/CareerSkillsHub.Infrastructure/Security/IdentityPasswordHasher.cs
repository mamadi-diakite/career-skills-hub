namespace CareerSkillsHub.Infrastructure.Security;

/// <summary>PBKDF2 hashing from ASP.NET Core Identity, without the rest of Identity.</summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // Identity's hasher takes a user instance but does not use it.
    private static readonly object AnyUser = new();

    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _hasher.HashPassword(AnyUser, password);
    }

    public bool Verify(string passwordHash, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(passwordHash);
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _hasher.VerifyHashedPassword(AnyUser, passwordHash, password) != PasswordVerificationResult.Failed;
    }
}
