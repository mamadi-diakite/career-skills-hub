namespace CareerSkillsHub.Application.Auth;

/// <summary>Random first password for an invited employee; it meets <see cref="PasswordPolicy"/>.</summary>
public static class TemporaryPassword
{
    // No 0/O, 1/l/I, so the password can be read out or copied by hand.
    private const string Letters = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";

    public static string Create() =>
        $"{RandomNumberGenerator.GetString(Letters, 6)}-{RandomNumberGenerator.GetString(Digits, 4)}-{RandomNumberGenerator.GetString(Letters, 6)}";
}
