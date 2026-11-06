namespace CareerSkillsHub.Application.Contracts.Auth;

/// <summary>Sign-up creates a company together with its first HR admin.</summary>
/// <param name="Headcount">Declared number of employees of the company.</param>
public sealed record SignUpRequest(
    string CompanyName,
    int Headcount,
    string DisplayName,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, CurrentUserResponse User);

/// <param name="Role">"HrAdmin", "Manager" or "Employee".</param>
/// <param name="EmployeeId">Employee record of the user, always set for managers and employees.</param>
public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid CompanyId,
    string CompanyName,
    Guid? EmployeeId);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);
