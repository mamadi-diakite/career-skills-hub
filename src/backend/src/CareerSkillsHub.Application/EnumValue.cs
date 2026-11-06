namespace CareerSkillsHub.Application;

public static class EnumValue
{
    /// <summary>Parses a name sent by a client, ignoring case; numbers outside the enum are refused.</summary>
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum =>
        Enum.TryParse(value?.Trim(), ignoreCase: true, out result) && Enum.IsDefined(result);
}
