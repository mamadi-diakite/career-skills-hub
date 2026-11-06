namespace CareerSkillsHub.Application.Auth;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static IReadOnlyList<string> Check(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return ["Password is required."];
        }

        var errors = new List<string>();
        if (password.Length < MinLength)
        {
            errors.Add($"Use at least {MinLength} characters.");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"Use at most {MaxLength} characters.");
        }

        if (!password.Any(char.IsLetter))
        {
            errors.Add("Use at least one letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Use at least one digit.");
        }

        return errors;
    }
}
