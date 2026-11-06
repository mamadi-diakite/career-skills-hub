namespace CareerSkillsHub.Domain.Tests;

internal static class TestDates
{
    public static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    public static DateTime Local => DateTime.SpecifyKind(Now, DateTimeKind.Local);
}
