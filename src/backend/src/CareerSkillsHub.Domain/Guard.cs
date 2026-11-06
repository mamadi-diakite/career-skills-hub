namespace CareerSkillsHub.Domain;

internal static class Guard
{
    public static string Required(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return MaxLength(value.Trim(), paramName, maxLength);
    }

    public static string? Optional(string? value, string paramName, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value.Trim(), paramName, maxLength);

    public static string Email(string? value, string paramName, int maxLength)
    {
        var email = Required(value, paramName, maxLength).ToLowerInvariant();

        return HasEmailShape(email) ? email : throw new ArgumentException("Email is not valid.", paramName);
    }

    public static bool IsEmail([NotNullWhen(true)] string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= maxLength
        && HasEmailShape(value.Trim().ToLowerInvariant());

    public static Guid NotEmpty(Guid value, string paramName) =>
        value == Guid.Empty ? throw new ArgumentException("Id is required.", paramName) : value;

    public static Guid? NotEmpty(Guid? value, string paramName) =>
        value == Guid.Empty ? throw new ArgumentException("Id cannot be empty.", paramName) : value;

    public static int Range(int value, string paramName, int min, int max) =>
        value < min || value > max
            ? throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.")
            : value;

    public static DateTime Utc(DateTime value, string paramName) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Date must be in UTC.", paramName);

    public static TEnum Defined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(paramName, value, "Unknown value.");

    private static bool HasEmailShape(string email)
    {
        var at = email.IndexOf('@');
        var domain = at < 0 ? string.Empty : email[(at + 1)..];

        return at > 0
            && at == email.LastIndexOf('@')
            && !email.Any(char.IsWhiteSpace)
            && domain.Contains('.')
            && !domain.StartsWith('.')
            && !domain.EndsWith('.');
    }

    private static string MaxLength(string value, string paramName, int maxLength) =>
        value.Length <= maxLength
            ? value
            : throw new ArgumentException($"Value must be at most {maxLength} characters.", paramName);
}
