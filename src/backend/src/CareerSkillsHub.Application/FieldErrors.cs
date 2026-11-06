namespace CareerSkillsHub.Application;

/// <summary>Collects validation messages by request field, in camelCase.</summary>
public sealed class FieldErrors : Dictionary<string, string[]>
{
    /// <returns>The trimmed value, or <c>null</c> when it is missing or too long.</returns>
    public string? Required(string field, string? value, int maxLength, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            this[field] = [$"{label} is required."];
            return null;
        }

        return MaxLength(field, value.Trim(), maxLength);
    }

    /// <returns>The trimmed value, or <c>null</c> when it is blank or too long.</returns>
    public string? Optional(string field, string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(field, value.Trim(), maxLength);

    public void Range(string field, int value, int min, int max, string label)
    {
        if (value < min || value > max)
        {
            this[field] = [$"{label} must be between {min} and {max}."];
        }
    }

    private string? MaxLength(string field, string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        this[field] = [$"Use at most {maxLength} characters."];
        return null;
    }
}
