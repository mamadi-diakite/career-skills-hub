namespace CareerSkillsHub.Application;

public enum ServiceStatus
{
    Ok = 0,

    /// <summary>The account of the caller no longer exists.</summary>
    Unauthorized = 1,

    Forbidden = 2,
    NotFound = 3,
    Invalid = 4,

    /// <summary>The change would break data that depends on it, for example deleting a department in use.</summary>
    Conflict = 5,
}

/// <param name="Errors">Messages by request field, in camelCase, when the status is <see cref="ServiceStatus.Invalid"/>.</param>
/// <param name="Message">Reason shown to the user for the other failures.</param>
public sealed record ServiceResult<T>(ServiceStatus Status, T? Value, IReadOnlyDictionary<string, string[]> Errors, string? Message)
{
    private static readonly IReadOnlyDictionary<string, string[]> NoErrors = new Dictionary<string, string[]>();

    public static ServiceResult<T> Ok(T value) => new(ServiceStatus.Ok, value, NoErrors, null);

    public static ServiceResult<T> Unauthorized() => new(ServiceStatus.Unauthorized, default, NoErrors, null);

    public static ServiceResult<T> Forbidden(string message) => new(ServiceStatus.Forbidden, default, NoErrors, message);

    public static ServiceResult<T> NotFound(string message) => new(ServiceStatus.NotFound, default, NoErrors, message);

    public static ServiceResult<T> Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(ServiceStatus.Invalid, default, errors, null);

    public static ServiceResult<T> Conflict(string message) => new(ServiceStatus.Conflict, default, NoErrors, message);
}
