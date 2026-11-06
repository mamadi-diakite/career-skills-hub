namespace CareerSkillsHub.Infrastructure.Persistence;

/// <summary>SQLite has no time zone support, so dates are stored as UTC and read back as UTC.</summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
