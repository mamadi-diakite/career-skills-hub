namespace CareerSkillsHub.Application.Contracts.Employees;

/// <param name="AccountRole">HrAdmin, Manager or Employee, or <c>null</c> when the employee has not been invited.</param>
public sealed record EmployeeSummaryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateOnly HireDate,
    Guid JobId,
    string JobTitle,
    Guid DepartmentId,
    string DepartmentName,
    Guid? ManagerId,
    string? ManagerName,
    string? AccountRole);

/// <param name="EndDate">Last day in the job, or <c>null</c> for the current job.</param>
public sealed record JobAssignmentResponse(
    Guid JobId,
    string JobTitle,
    Guid DepartmentId,
    string DepartmentName,
    DateOnly StartDate,
    DateOnly? EndDate);

/// <param name="JobHistory">Most recent first.</param>
public sealed record EmployeeDetailsResponse(EmployeeSummaryResponse Employee, IReadOnlyList<JobAssignmentResponse> JobHistory);

public sealed record CreateEmployeeRequest(
    string FirstName,
    string LastName,
    string Email,
    DateOnly? HireDate,
    Guid JobId,
    Guid DepartmentId,
    Guid? ManagerId);

public sealed record JobChangeRequest(Guid JobId, Guid DepartmentId, DateOnly? EffectiveDate);

/// <param name="Role">HrAdmin, Manager or Employee.</param>
public sealed record InvitationRequest(string Role);

/// <param name="TemporaryPassword">Shown once; the employee changes it after the first login.</param>
public sealed record InvitationResponse(Guid EmployeeId, string Email, string Role, string TemporaryPassword);
